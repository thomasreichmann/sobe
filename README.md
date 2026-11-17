# Sobe

Global Solution 2026/2 (CitizenTech) da graduação em Engenharia de Software da FIAP, turma 3ESOA.

Quem usa cadeira de rodas, tem dificuldade para andar ou leva um carrinho de bebê só descobre que o
elevador da estação está parado quando chega nele. O **Sobe** mostra, antes de a pessoa sair de casa,
se os elevadores, as escadas rolantes e as plataformas de cada estação estão funcionando. Sensores
instalados nos equipamentos detectam a parada, o aplicativo avisa quem acompanha aquela estação e
indica a estação acessível mais próxima na mesma linha.

**Integrante:** Thomas de Almeida Reichmann, RM 554812, turma 3ESOA.

**Vídeo pitch:** <LINK-DO-PITCH>

O PDF com os entregáveis de cada parte está em `entrega/`. Ele se conecta aos ODS 10 (redução das
desigualdades) e 11 (cidades e comunidades sustentáveis, meta 11.2: transporte acessível para todos).

## O que há aqui

| Pasta | Parte | Conteúdo |
|---|---|---|
| `banco/` | Banco de dados | `sobe.sql` (tabelas e carga inicial, Oracle) e `consultas.sql` (7 consultas de uso) |
| `api/` | API RESTful e testes | ASP.NET Core 8 em C#, com Entity Framework Core; testes automatizados; `swagger.json` |
| `mobile/` | Frontend mobile | App Android em Kotlin e Jetpack Compose, que consome a API com Retrofit e guarda a última situação com Room |
| `iot/` | Simulação de IoT | `simulador.py`, que faz o papel dos sensores e envia as leituras para a API |
| `evidencias/` | Plano de testes | Saídas do banco, dos testes, da API e do simulador, e as telas do app |
| `entrega/` | PDF | O relatório e o que o gera |
| `scripts/` | | Geração das evidências e do pacote da entrega |

## Como rodar

Precisa de Docker com Compose para o banco e a API, Python 3 para o simulador e Android Studio para o
app. Os testes da API pedem o SDK do .NET 8.

### 1. Banco e API

```sh
docker compose up -d --build --wait
```

Sobe um Oracle Free, roda `banco/sobe.sql` nele e publica a API em <http://localhost:5080>. A primeira
vez leva alguns minutos, por causa da imagem do Oracle. A documentação interativa (Swagger) fica em
<http://localhost:5080/swagger>; o mesmo conteúdo está em `api/swagger.json`.

Usuários da carga inicial:

| Perfil | E-mail | Senha |
|---|---|---|
| Administrador (operadora) | `admin@sobe.app` | `Admin@2026` |
| Cidadão | `ana@sobe.app` | `Cidadao@2026` |
| Cidadão | `carlos@sobe.app` | `Cidadao@2026` |

No Swagger, chame `POST /api/auth/login`, copie o `token` e cole em **Authorize**. Para recomeçar com o
banco limpo: `docker compose down -v`.

### 2. Sensores simulados

```sh
python3 iot/simulador.py
```

O simulador busca os sensores na API e envia uma leitura de cada um a cada 5 segundos. No terminal
dele, `parar 41` faz o elevador 41 (Sé, Linha 1) parar de se mover, `porta 41` trava a porta aberta e
`voltar 41` normaliza; `lista` mostra os equipamentos. Em cerca de 20 segundos a leitura passa do
limite, a API abre o alerta e o app avisa. Para rodar sem digitar nada:

```sh
python3 iot/simulador.py --estacoes "Sé" --parar 41 --voltar-no-envio 4 --ciclos 4 --intervalo 2 --aceleracao 240
```

### 3. App Android

Abra a pasta `mobile/` no Android Studio e rode o módulo `app` num emulador (API 26 ou mais nova). Pela
linha de comando, com um emulador ligado:

```sh
cd mobile && ./gradlew installDebug
```

No emulador o app já aponta para `http://10.0.2.2:5080/`, que é o `localhost` do computador. Num celular
de verdade, toque na engrenagem da tela de entrada e informe o endereço do computador na rede.

Sem conexão, o app mostra a última situação que guardou no aparelho (Room), com o horário. Para ver:
carregue a lista, rode `docker compose stop api` e reabra o app.

### 4. Testes automatizados

```sh
cd api && dotnet test
```

São testes de unidade e de API. Os de API sobem a aplicação inteira em memória com um banco SQLite
novo para cada teste, então não dependem do Docker.

### Sem Docker

Rode `banco/sobe.sql` num Oracle 12c ou mais novo e suba a API apontando para ele:

```sh
cd api/src/Sobe.Api
ConnectionStrings__Oracle="User Id=USUARIO;Password=SENHA;Data Source=SERVIDOR:1521/SERVICO" dotnet run
```

## API

Três camadas, uma pasta para cada: `Controllers` (HTTP), `Services` (regras) e `Repositories` (acesso
ao banco pelo Entity Framework Core). As entidades do banco são os recursos da API.

| Método e rota | Quem pode | O que faz |
|---|---|---|
| `POST /api/auth/registrar` | todos | Cria uma conta de cidadão |
| `POST /api/auth/login` | todos | Devolve o token JWT |
| `GET /api/auth/eu` | autenticado | Dados de quem está logado |
| `GET /api/linhas` | todos | Linhas |
| `GET /api/estacoes` | todos | Estações com a situação dos equipamentos (busca e filtros) |
| `GET /api/estacoes/{id}` | todos | Equipamentos, alertas abertos e estação alternativa |
| `POST` `PUT` `DELETE /api/estacoes` | administrador | Cadastro de estações |
| `POST /api/estacoes/{id}/equipamentos` | administrador | Novo equipamento, já com os sensores |
| `GET /api/equipamentos/{id}` | todos | Situação de um equipamento |
| `PUT` `DELETE /api/equipamentos/{id}` | administrador | Situação (manutenção, parado, operando) e remoção |
| `GET` `PUT` `DELETE /api/favoritos` | autenticado | Estações que o usuário acompanha |
| `GET /api/alertas` | autenticado | Paradas em aberto, de todas as estações ou só das favoritas |
| `GET` `POST` `PUT` `DELETE /api/relatos` | autenticado | Avisos do cidadão; cada um vê e altera só os seus |
| `PUT /api/relatos/{id}/status` | administrador | Confirma, resolve ou descarta um aviso |
| `GET /api/sensores` | administrador ou sensores | Sensores e limites |
| `PUT /api/sensores/{id}` | administrador | Ajusta o limite ou desativa o sensor |
| `POST /api/leituras` | sensores | Recebe uma leitura e aplica a regra de parada |
| `GET /api/painel` | administrador | Resumo da operação em 30 dias |

## Sensores

| Sensor | O que mede | Limite padrão | Efeito no sistema |
|---|---|---|---|
| Movimento (acelerômetro na cabine ou nos degraus) | Minutos seguidos sem o equipamento se mover | 15 min | Acima do limite, o equipamento passa a **parado** e um alerta é aberto |
| Porta (sensor magnético) | Segundos seguidos com a porta aberta | 120 s | Acima do limite, indica porta travada: mesmo efeito |

O alerta só é encerrado quando todos os sensores do equipamento voltam a ficar dentro do limite. Em
manutenção programada as leituras são guardadas, mas não geram alerta. Com um elevador ou plataforma
fora de serviço, a estação deixa de contar como acessível e a API passa a indicar a alternativa.

## Segurança

- **Senha em hash:** PBKDF2 com SHA-256, salt de 16 bytes por usuário e 600 mil iterações
  (`Security/SenhaHasher.cs`). O banco nunca recebe a senha.
- **Perfis:** token JWT com validade de 2 horas e o perfil do usuário; as rotas de administração
  exigem `ADMIN`. Um cidadão não lê nem altera o aviso de outro.
- **Sensores com credencial própria:** as leituras só entram com a chave de dispositivo, que não dá
  acesso ao resto da API.
- **Validação de entrada:** todo corpo de requisição é validado antes de chegar à regra de negócio, e
  as mensagens de erro saem no formato padrão `application/problem+json`.
- **SQL injection:** todo acesso ao banco passa pelo Entity Framework Core, com parâmetros.
- **Força bruta:** login e cadastro aceitam 10 tentativas por minuto por endereço; o login responde a
  mesma mensagem para e-mail inexistente e senha errada.
- **Segredos fora do código:** chave do JWT, chave dos sensores e senha do banco vêm de variáveis de
  ambiente (`.env.exemplo`). Os valores do `compose.yaml` servem só para rodar na própria máquina.
- **App:** o tráfego sem TLS só é permitido na versão de depuração; a de produção exige HTTPS.

## Evidências e PDF

```sh
bash scripts/evidencias.sh        # recria o ambiente e grava as saídas em evidencias/
bash scripts/capturas-app.sh      # telas do app, com um emulador ligado
cd entrega && npm install && node pdf.mjs
bash scripts/empacotar.sh         # monta o .zip da entrega
```

A fonte do app e do relatório é a Atkinson Hyperlegible, do Braille Institute, sob a licença SIL Open
Font (`mobile/app/LICENCA-DA-FONTE.txt`).
