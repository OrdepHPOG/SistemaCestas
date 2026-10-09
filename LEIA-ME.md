# Sistema de Cestas Básicas — dados somente em memória

Projeto C# de console com POO para Visual Studio 2022 e .NET Framework 4.8.

## Executar

1. Extraia o ZIP inteiro e abra `SistemaCestas.sln` no Visual Studio 2022.
2. Tenha instalado o componente de desenvolvimento desktop com .NET e o pacote de direcionamento do .NET Framework 4.8.
3. Pressione Ctrl+F5 para compilar e executar.

Alternativamente, execute `Compilar.bat` no Windows com .NET Framework 4.8. Ele gera `Executavel/SistemaCestas.exe`. Pelo Visual Studio, a compilação Debug gera `SistemaCestas/bin/Debug/SistemaCestas.exe`.

Todo o código da aplicação está em `SistemaCestas/Program.cs`, com as classes separadas dentro desse arquivo.

## Registro de conta e armazenamento

A primeira tela solicita o REGISTRO da conta do administrador: nome, criação de senha e confirmação. Depois do registro, o menu abre diretamente, sem solicitar login. Uma confirmação incorreta ou senha vazia permite tentar novamente.

Administrador, produtos, composição, famílias, visitas e entregas são guardados em coleções `LinkedList<T>`, somente na memória RAM. As associações entre objetos usam IDs internos. A senha é mantida como hash em memória.

**Fechar o programa perde todos os dados da sessão. A próxima execução começa novamente pelo registro de conta, com as demais listas vazias.** Não há gravação nem leitura de arquivos de dados, XML, JSON, banco, backup ou arquivo de bloqueio. Arquivos de dados de versões anteriores não são lidos nem apagados.

Os arquivos de código, projeto e executáveis continuam existindo no disco; eles não armazenam os cadastros feitos no console.

## Menus e regras

As opções permitem cadastrar/adicionar e retirar produtos, consultar estoque/cestas, informar a composição, cadastrar/consultar famílias, agendar/consultar visitas, definir autorização e registrar/consultar entregas.

A composição da cesta é informada pela organização na opção 4, conforme RD02. Não existe receita predefinida. Sem composição, a retirada fica bloqueada. O cálculo considera o produto que permite montar a menor quantidade de cestas inteiras.

Famílias precisam de autorização manual. As opções 10 e 11 representam uma mesma operação: verificam autorização, baixam os produtos de uma cesta e registram a família e a data. Use somente uma das opções para cada atendimento. Não foram acrescentadas regras de limite mensal, expiração de autorização ou visita obrigatória antes da entrega.

Classes do diagrama: Estoque, Cestas, Adm, Familias, Produto e Entrega. Visita atende RF04/RD06; ItemComposicao atende RD02. DadosSistema, Validacao e Program são suporte técnico. Datas são representadas por DateTime.Date, sem horário.

A mudança para dados voláteis substitui a persistência da versão anterior por solicitação do usuário. O cadastro inicial identifica o administrador da sessão e abre o menu diretamente.

## O que é a pasta .vs?

É uma pasta criada automaticamente pelo Visual Studio, com configurações locais, cache e informações de trabalho da solução. Não é o banco de dados do sistema e não guarda os cadastros do console. Pode ser apagada com o Visual Studio fechado; ele recria o necessário ao abrir a solução. Algumas preferências locais podem ser perdidas. Ela não está incluída neste ZIP.

## Verificações

`Testar.bat` compila e executa testes de domínio: hash da senha, reposição de produto, cálculo das cestas, bloqueio por falta de autorização/estoque, baixa e registro de entrega, datas sem horário, coleções LinkedList e criação de uma nova sessão vazia. Os testes também trabalham somente em memória.

Código e referências do projeto foram revisados e foi verificada a ausência de operações de arquivo na aplicação. Não há compilador C# neste ambiente: a compilação e a execução dos testes ainda precisam ser feitas no Windows. O ZIP não inclui executável pré-compilado.
