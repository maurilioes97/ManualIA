# Manual IA — versão simples

Implementação acadêmica dos casos de uso CDU01 a CDU08:

1. Fazer login.
2. Gerenciar usuários.
3. Gerenciar manuais.
4. Enviar PDF ou DOCX e salvar os trechos extraídos.
5. Pesquisar manuais disponíveis para consulta.
6. Abrir um chat do manual e registrar perguntas.
7. Gerar respostas com Gemini e apresentar referências.
8. Consultar o histórico de conversas e continuar um chat anterior.

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
- Somente administradores gerenciam usuários e manuais; funcionários consultam manuais processados e usam o chat.
- E-mail de usuário não pode ser repetido.
- O administrador não pode excluir a própria conta.
- Título e descrição do manual são obrigatórios.
- Upload aceita apenas PDF e DOCX de até 10 MB.
- Substituição de arquivo exige confirmação.
- Texto extraído é persistido na tabela `ChunksManuais`.

## Gemini (CDU07)

Crie uma chave no [Google AI Studio](https://aistudio.google.com/apikey) usando um projeto no nível gratuito.
O modelo padrão é `gemini-3.5-flash-lite`, configurável em `Gemini:Modelo` no `appsettings.json`.
A gratuidade depende do nível do projeto e da quota disponível no Google; o código não altera sua cobrança.
Confira os [preços e modelos elegíveis](https://ai.google.dev/gemini-api/docs/pricing).
Use documentos públicos nos testes: no nível gratuito, o conteúdo pode ser usado para melhorar produtos do Google.

Para facilitar o desenvolvimento local, copie a chave para o arquivo ignorado pelo Git
`backend/ManualIA.Api/appsettings.Local.json`:

```json
{
  "GEMINI_API_KEY": "sua-chave"
}
```

O arquivo é carregado somente no ambiente `Development`. Reinicie o backend depois de alterar a chave.
No deploy, configure `GEMINI_API_KEY` como variável de ambiente; ela tem prioridade sobre o arquivo local.

No PowerShell que iniciará o backend, informe a chave sem gravá-la no histórico do terminal:

```powershell
$chaveGemini = Read-Host 'Chave da API Gemini' -AsSecureString
$env:GEMINI_API_KEY = [System.Net.NetworkCredential]::new('', $chaveGemini).Password
dotnet run --project backend/ManualIA.Api
```

Reinicie o backend se ele já estava em execução. A variável vale para esse terminal e os processos iniciados por ele.
Não coloque a chave no React, no Git ou no `appsettings.json`. O projeto não carrega arquivos `.env` automaticamente.

Se o terminal mostrar `Gemini retornou HTTP 404`, confira o modelo configurado e sua disponibilidade no
projeto Google AI Studio. O Google [restringe o acesso aos modelos 2.5](https://ai.google.dev/gemini-api/docs/deprecations)
a usuários que já os utilizavam. Verifique também se `Gemini__Modelo` no ambiente ou uma configuração
de desenvolvimento está sobrescrevendo o `appsettings.json`. Após ajustar, reinicie o backend no mesmo terminal da chave.
O chat identifica esse erro como `modelo_indisponivel`, preservando os trechos sem repetir a chamada.

Entre como funcionário, selecione **Ver detalhes → Abrir chat** e faça uma pergunta com termos presentes no manual.
O backend envia somente a pergunta e até cinco trechos relevantes ao Gemini. A busca continua sendo lexical,
sem embeddings; perguntas que usem apenas sinônimos podem não localizar o conteúdo.
Perguntas e respostas são salvas juntas, incluindo uma cópia dos trechos citados.
As chamadas não enviam mensagens anteriores da conversa, conforme CDU07/RN03.

O funcionário pode abrir **Histórico** no cabeçalho para consultar somente as próprias conversas,
visualizar perguntas e respostas anteriores e continuar o chat vinculado ao mesmo manual.

Sem fontes, sem chave, com HTTP 429, falha de rede, timeout, recusa ou saída inválida,
o chat mostra um aviso do sistema e os trechos disponíveis. Não há novas tentativas automáticas.
O timeout padrão é 30 segundos (`Gemini:TimeoutSegundos`, entre 1 e 60); respostas de IA não têm garantia de 3 segundos.
A validação verifica o formato e os IDs das referências, mas não comprova a correção factual da resposta.

### Verificação sem consumir API

Da raiz do repositório:

```powershell
dotnet run --project tests/ManualIA.Verificacoes -c Release
```

Para verificar também persistência e isolamento de usuários/manuais, com o banco local iniciado e
as tabelas já criadas pela API:

```powershell
dotnet run --project tests/ManualIA.Verificacoes -c Release -- --banco
```

Os testes usam HTTP simulado e chave fictícia. O teste de banco cria fixtures próprias dentro de uma
transação e executa rollback; sequências de IDs podem avançar. Não envia documentos reais ao Gemini.
