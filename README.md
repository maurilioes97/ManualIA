# Manual IA — versão simples

Implementação acadêmica dos quatro casos de uso:

1. Fazer login.
2. Gerenciar usuários.
3. Gerenciar manuais.
4. Enviar PDF ou DOCX e salvar os trechos extraídos.

## Tecnologias

- ASP.NET Core Web API (.NET 10)
- React com TypeScript e Vite
- PostgreSQL e Adminer no Docker

O backend foi mantido propositalmente pequeno. Os controllers recebem as requisições e delegam as regras para serviços concretos, que acessam o `AppDbContext` diretamente. Não há interfaces ou camada de Repository.

## Como executar

Pré-requisitos: .NET 10, Node.js e Docker Desktop.

### 1. Banco de dados

```powershell
docker compose up -d
```

O Adminer ficará em `http://localhost:8080`. Para entrar, use:

- Sistema: PostgreSQL
- Servidor: `postgres`
- Usuário: `manualia`
- Senha: `manualia123`
- Base de dados: `manualia`

### 2. Backend

```powershell
dotnet run --project backend/ManualIA.Api
```

A API ficará em `http://localhost:5080`.

### 3. Frontend

Em outro terminal:

```powershell
cd frontend
npm install
npm run dev
```

Abra `http://localhost:5173`.

## Acesso inicial

- E-mail: `admin@manualia.com`
- Senha: `Admin123!`

Esses dados e a chave JWT estão no `appsettings.json` somente para facilitar a execução acadêmica local.

## Validações principais

- Login com senha armazenada como hash.
- Somente administradores acessam usuários e manuais.
- E-mail de usuário não pode ser repetido.
- O administrador não pode excluir a própria conta.
- Título e descrição do manual são obrigatórios.
- Upload aceita apenas PDF e DOCX de até 10 MB.
- Substituição de arquivo exige confirmação.
- Texto extraído é persistido na tabela `ChunksManuais`.
