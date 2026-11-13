#!/usr/bin/env python3
import argparse
import json
import os
import queue
import random
import sys
import threading
import time
import urllib.error
import urllib.request

AJUDA = """Comandos (digite e tecle Enter):
  parar <equipamento>   a cabine para de se mover
  porta <equipamento>   a porta trava aberta
  voltar <equipamento>  o equipamento volta ao normal
  lista                 equipamentos simulados e o modo de cada um
  sair"""


class Api:
    def __init__(self, base, chave):
        self.base = base.rstrip("/")
        self.chave = chave

    def chamar(self, metodo, caminho, corpo=None):
        dados = json.dumps(corpo).encode() if corpo is not None else None
        pedido = urllib.request.Request(self.base + caminho, data=dados, method=metodo)
        pedido.add_header("X-Chave-Dispositivo", self.chave)
        pedido.add_header("Content-Type", "application/json")
        with urllib.request.urlopen(pedido, timeout=10) as resposta:
            return json.load(resposta)


class Equipamento:
    def __init__(self, local):
        self.id = local["equipamentoId"]
        self.nome = f'{local["tipo"].replace("_", " ").capitalize()} {self.id} · {local["estacao"]} (Linha {local["linhaNumero"]})'
        self.sensores = {}
        self.modo = "normal"
        self.sem_movimento = 0.0
        self.porta_aberta = 0.0
        self.status = None

    def proxima_leitura(self, tipo, limite, minutos, segundos):
        if tipo == "MOVIMENTO":
            if self.modo == "normal":
                self.sem_movimento = random.uniform(0, limite * 0.5)
            else:
                self.sem_movimento += minutos
            return round(self.sem_movimento, 1)
        if self.modo == "porta":
            self.porta_aberta += segundos
        else:
            self.porta_aberta = random.uniform(limite * 0.05, limite * 0.4)
        return round(self.porta_aberta)


def carregar(api, estacoes):
    equipamentos = {}
    for sensor in api.chamar("GET", "/api/sensores"):
        local = sensor["local"]
        if not sensor["ativo"] or (estacoes and local["estacao"].lower() not in estacoes):
            continue
        equipamento = equipamentos.setdefault(local["equipamentoId"], Equipamento(local))
        equipamento.sensores[sensor["tipo"]] = sensor
    return equipamentos


def enviar(api, equipamento, minutos, segundos):
    for tipo, sensor in equipamento.sensores.items():
        valor = equipamento.proxima_leitura(tipo, sensor["limite"], minutos, segundos)
        try:
            leitura = api.chamar("POST", "/api/leituras", {"sensor": sensor["codigo"], "valor": valor})
        except urllib.error.HTTPError as erro:
            print(f'  {sensor["codigo"]}: leitura recusada ({erro.code})')
            continue
        status = leitura["statusDoEquipamento"]
        if leitura["acimaDoLimite"]:
            print(f'  {sensor["codigo"]}: {valor:g} {sensor["unidade"]} passou do limite de {sensor["limite"]:g} {sensor["unidade"]}')
        if equipamento.status not in (None, status):
            print(f"  >>> {equipamento.nome}: {equipamento.status} -> {status}")
        equipamento.status = status


def aplicar(comando, equipamentos):
    partes = comando.split()
    if not partes:
        return True
    acao = partes[0].lower()
    if acao == "sair":
        return False
    if acao == "lista":
        for equipamento in equipamentos.values():
            print(f"  {equipamento.id:>4}  {equipamento.modo:<7} {equipamento.nome}")
        return True
    modos = {"parar": "parado", "porta": "porta", "voltar": "normal"}
    if acao not in modos or len(partes) != 2 or not partes[1].isdigit() or int(partes[1]) not in equipamentos:
        print(AJUDA)
        return True
    equipamento = equipamentos[int(partes[1])]
    equipamento.modo = modos[acao]
    print(f"  {equipamento.nome}: modo {equipamento.modo}")
    return True


def ler_teclado(fila):
    for linha in sys.stdin:
        fila.put(linha.strip())
    fila.put(None)


def main():
    opcoes = argparse.ArgumentParser(description="Simula os sensores de movimento e de porta dos equipamentos do Sobe.")
    opcoes.add_argument("--api", default=os.environ.get("SOBE_API", "http://localhost:5080"))
    opcoes.add_argument("--chave", default=os.environ.get("SOBE_CHAVE_DISPOSITIVO", "sensores-do-sobe-demo"))
    opcoes.add_argument("--intervalo", type=float, default=5, help="segundos entre os envios")
    opcoes.add_argument("--aceleracao", type=float, default=60, help="quantos segundos simulados cabem em um segundo real")
    opcoes.add_argument("--estacoes", default="", help="nomes separados por vírgula; vazio simula todas")
    opcoes.add_argument("--parar", type=int, action="append", default=[], metavar="EQUIPAMENTO")
    opcoes.add_argument("--porta", type=int, action="append", default=[], metavar="EQUIPAMENTO")
    opcoes.add_argument("--ciclos", type=int, default=0, help="encerra depois de N envios; 0 roda até sair")
    opcoes.add_argument("--voltar-no-envio", type=int, default=0, metavar="N", help="no envio N, tudo volta ao normal")
    args = opcoes.parse_args()

    api = Api(args.api, args.chave)
    estacoes = {nome.strip().lower() for nome in args.estacoes.split(",") if nome.strip()}
    while True:
        try:
            equipamentos = carregar(api, estacoes)
            break
        except (urllib.error.URLError, OSError) as erro:
            print(f"API indisponível em {args.api} ({erro}); tentando de novo em 5 s")
            time.sleep(5)
    if not equipamentos:
        sys.exit("Nenhum sensor ativo para simular.")

    for numero in args.parar:
        aplicar(f"parar {numero}", equipamentos)
    for numero in args.porta:
        aplicar(f"porta {numero}", equipamentos)

    sensores = sum(len(e.sensores) for e in equipamentos.values())
    print(f"Simulando {sensores} sensores de {len(equipamentos)} equipamentos em {args.api}, um envio a cada {args.intervalo:g} s.")
    fila = queue.Queue()
    if sys.stdin.isatty():
        print(AJUDA)
        threading.Thread(target=ler_teclado, args=(fila,), daemon=True).start()

    segundos = args.intervalo * args.aceleracao
    ciclo = 0
    while args.ciclos == 0 or ciclo < args.ciclos:
        ciclo += 1
        print(f'[{time.strftime("%H:%M:%S")}] envio {ciclo}')
        if ciclo == args.voltar_no_envio:
            for equipamento in equipamentos.values():
                equipamento.modo = "normal"
        for equipamento in equipamentos.values():
            try:
                enviar(api, equipamento, segundos / 60, segundos)
            except (urllib.error.URLError, OSError) as erro:
                print(f"  API indisponível ({erro})")
                break
        fim = time.monotonic() + args.intervalo
        while time.monotonic() < fim:
            try:
                comando = fila.get(timeout=max(0.05, fim - time.monotonic()))
            except queue.Empty:
                continue
            if comando is None or not aplicar(comando, equipamentos):
                return


if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        pass
