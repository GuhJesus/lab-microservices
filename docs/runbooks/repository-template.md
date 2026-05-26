# Versionamento e Template do Projeto

## Objetivo

Usar este laboratorio como repositorio-base, com historico de versoes, publicacao em nuvem e criacao controlada de variacoes.

## Modelo recomendado

1. `main`: branch da base estavel.
2. `feature/...`: mudancas pequenas e isoladas.
3. `release/...`: preparacao de entregas maiores, se necessario.
4. `tag`: marco estavel da base, por exemplo `v1.0-base`.

## Quando usar branch

Use branch quando a variacao ainda faz parte do mesmo produto ou quando a mudanca pode voltar para a base sem conflito conceitual.

Exemplos:

- trocar o consumidor do worker de log para webhook
- adicionar cache distribudo extra
- incluir telemetria adicional

## Quando criar outro repositorio

Crie um novo repositorio quando a variacao virar outro projeto, cliente ou linha de produto.

Exemplos:

- base para cliente A com regras proprias
- base para estudo de saga/orquestracao
- base para notificacao por webhook em vez de worker generico

## Fluxo sugerido

1. Evolua e estabilize a base.
2. Faça commit pequeno e claro.
3. Crie tag de versao da base.
4. Gere uma variacao por branch ou novo repositorio.
5. Reaplique na base apenas o que for realmente generico.

## Sequencia minima para subir a base

```powershell
git add .
git commit -m "chore: bootstrap base microservices lab"
git branch -M main
git remote add origin https://github.com/<seu-usuario>/lab-microservices.git
git push -u origin main
git tag v1.0-base
git push origin v1.0-base
```

## Como criar uma nova variacao

### Opcao 1: branch

```powershell
git checkout -b feature/nova-variacao
```

### Opcao 2: novo repositorio

1. Crie um novo projeto vazio no provedor Git.
2. Clone a base em outra pasta ou use a funcao de template do provedor.
3. Ajuste nome, documentacao e configuracoes da nova variacao.
4. Publique no novo remoto.

## Boas praticas

- Nao commite segredos reais.
- Nao use copias soltas como estrategia principal.
- Mantenha o `README` explicando o que e base e o que e customizacao.
- Prefira configuracao por ambiente em vez de editar codigo.
- Crie tags para versoes-base antes de grandes derivacoes.
