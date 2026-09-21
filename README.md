# API de vendas

API em .NET 8 com PostgreSQL, EF Core, MediatR, AutoMapper e FluentValidation.
Implementa CRUD de vendas, cancelamento, descontos por produto, filtros, paginação,
ordenação e logs. Users e Auth foram preservados do template.

[Enunciado original](.doc/challenge.md) · [Documentação do template](.doc/overview.md)

## Executar

Requisitos: SDK e runtime .NET 8, Docker com Compose e portas 5432 e 5119 disponíveis.

Na raiz do repositório:

```bash
dotnet tool restore
dotnet restore template/backend/src/Ambev.DeveloperEvaluation.WebApi/Ambev.DeveloperEvaluation.WebApi.csproj

cd template/backend
docker compose up -d ambev.developerevaluation.database
docker compose ps
```

Aguarde o banco ficar `healthy`. A conexão está em
`template/backend/src/Ambev.DeveloperEvaluation.WebApi/appsettings.json`:
banco `developer_evaluation`, usuário `developer` e senha
`local_development_password`, usados somente no ambiente local.
Para sobrescrever a conexão, configure `ConnectionStrings__DefaultConnection`
antes das migrations e da execução da API.

Ainda em `template/backend`, aplique as migrations e inicie a API:

```bash
cd src/Ambev.DeveloperEvaluation.WebApi
dotnet ef database update --project ../Ambev.DeveloperEvaluation.ORM --startup-project .
dotnet run --launch-profile http
```

A factory de migrations lê as configurações da pasta atual, por isso execute
o comando dentro da WebApi.

Abra o [Swagger](http://localhost:5119/swagger) para consultar os contratos e
testar os endpoints. As rotas de vendas não exigem autenticação neste protótipo.

## Testes

Na raiz do repositório:

```bash
dotnet test template/backend/tests/Ambev.DeveloperEvaluation.Unit/Ambev.DeveloperEvaluation.Unit.csproj
```

Testes com xUnit, Bogus e NSubstitute cobrem descontos, limites, arredondamento,
atualização, cancelamento e criação de vendas. Não precisam do banco em execução.

## Endpoints

| Método | Rota | Operação |
|---|---|---|
| POST | `/api/sales` | Criar |
| GET | `/api/sales` | Listar |
| GET | `/api/sales/{id}` | Consultar |
| PUT | `/api/sales/{id}` | Atualizar |
| PATCH | `/api/sales/{id}/cancel` | Cancelar |
| DELETE | `/api/sales/{id}` | Excluir |

Use o identificador retornado pelo POST nas demais operações.
Erros usam `type`, `error` e `detail`, com status 400 para dados inválidos,
404 para venda inexistente, 409 para conflito e 500 para falha inesperada.

## Regras e decisões

- De 1 a 3 unidades: sem desconto; de 4 a 9: 10%; de 10 a 20: 20%.
  Foi seguida a indicação “4+” do enunciado para quatro unidades.
- Quantidades fora de 1 a 20 e produtos repetidos na mesma venda são rejeitados.
- Preço positivo, com até duas casas decimais e limite de 1000000000.
- Descontos e totais são calculados pela aplicação usando `decimal`.
  `discount` é o valor monetário; o arredondamento usa duas casas e `AwayFromZero`.
- Número da venda único e data em UTC, com sufixo `Z`.
- Cliente, filial e produto são armazenados com identificador externo e nome.
- O PUT substitui os dados editáveis e os itens, recalculando os totais.
- Cancelar preserva os valores históricos e bloqueia atualizações. Excluir remove
  a venda e seus itens.

## Listagem

- `_page`: padrão 1; `_size`: padrão 10, máximo 100.
- `_order`: padrão `saleDate desc`. Aceita `saleNumber`, `saleDate`,
  `customerName` e `totalAmount`, com `asc` ou `desc`, separados por vírgula.
- Filtros: `saleNumber`, `customerName`, `customerId`, `branchId`,
  `isCancelled`, `_minTotalAmount`, `_maxTotalAmount`,
  `_minSaleDate` e `_maxSaleDate`.
- Filtros textuais aceitam `*` e diferenciam maiúsculas de minúsculas.
  Filtros distintos são combinados com AND; cada parâmetro aceita um valor.
- A resposta contém `data`, `totalItems`, `currentPage` e `totalPages`.

Exemplo: `GET /api/sales?customerName=Cliente*&isCancelled=false&_minTotalAmount=700`.

## Estrutura

Dentro de `template/backend/src`: Domain contém entidades e contratos;
Application organiza os casos de uso de vendas em pastas próprias; ORM contém
persistência e migrations; WebApi contém controllers e middleware; IoC registra
as dependências. Os testes ficam em `template/backend/tests`.
