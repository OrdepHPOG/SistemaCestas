using System;
using System.Linq;
using System.Collections.Generic;
using SistemaCestas;

internal static class Testes
{
    private static int verificacoes;
    private static void Verificar(bool condicao, string nome)
    { if (!condicao) throw new Exception("FALHOU: " + nome); verificacoes++; Console.WriteLine("OK: " + nome); }
    private static void Rejeita(Action acao, string nome)
    {
        bool rejeitou = false;
        try { acao(); } catch (InvalidOperationException) { rejeitou = true; }
        Verificar(rejeitou, nome);
    }
    public static int Main()
    {
        try
        {
            var d = new DadosSistema(new Adm("teste", "senha de teste"));
            Verificar(d.Administrador.Autenticar("teste", "senha de teste"), "Autenticacao valida");
            Verificar(!d.Administrador.Autenticar("teste", "incorreta"), "Senha incorreta rejeitada");
            d.Estoque.CadastrarProduto("Produto A", 10); d.Estoque.CadastrarProduto("Produto B", 7);
            var a = d.Estoque.Produtos.First(); var b = d.Estoque.Produtos.Last();
            d.Estoque.CadastrarProduto("produto a", 2);
            Verificar(a.Quantidade == 12 && d.Estoque.Produtos.Count() == 2, "Reposicao do mesmo produto");
            var itens = new LinkedList<ItemComposicao>();
            itens.AddLast(new ItemComposicao(a.Id, 2)); itens.AddLast(new ItemComposicao(b.Id, 3));
            d.Estoque.DefinirComposicao(itens);
            Verificar(d.Estoque.CalcularCestas() == 2, "Calculo pelo produto limitante");
            d.Administrador.CadastrarFamilia(d.Familias, "Nome", "Sobrenome", "Endereco");
            var familia = d.Familias.First.Value;
            var cestas = new Cestas(); var data = new DateTime(2026, 9, 30);
            Rejeita(() => cestas.RetirarCesta(d.Estoque, familia, data, d.Entregas), "Familia nao autorizada bloqueada");
            Verificar(a.Quantidade == 12 && d.Entregas.Count == 0, "Bloqueio nao altera estoque nem entregas");
            Rejeita(() => d.Estoque.RetirarProduto(a.Id, 13), "Retirada acima do estoque bloqueada");
            d.Administrador.AutorizarFamilia(familia, true);
            d.Administrador.AgendarVisita(d.Visitas, familia, data.AddHours(10));
            cestas.RetirarCesta(d.Estoque, familia, data.AddHours(15), d.Entregas);
            Verificar(a.Quantidade == 10 && b.Quantidade == 4 && d.Entregas.Count == 1, "Retirada baixa produtos e registra entrega");
            Verificar(d.Entregas.First.Value.Data == data && d.Visitas.First.Value.Data == data, "Datas sem horario");
            Verificar(d.Familias is LinkedList<Familias> && d.Entregas is LinkedList<Entrega>
                && d.Visitas is LinkedList<Visita>, "Colecoes em LinkedList");
            cestas.RetirarCesta(d.Estoque, familia, data, d.Entregas);
            Rejeita(() => cestas.RetirarCesta(d.Estoque, familia, data, d.Entregas), "Cesta indisponivel bloqueada");
            Verificar(d.Entregas.Count == 2, "Falha nao registra entrega extra");
            var novaSessao = new DadosSistema(new Adm("outro", "outra senha"));
            Verificar(novaSessao.Administrador.Nome == "outro", "Conta registrada na nova sessao");
            Verificar(!novaSessao.Estoque.Produtos.Any() && !novaSessao.Estoque.Composicao.Any()
                && novaSessao.Familias.Count == 0 && novaSessao.Visitas.Count == 0 && novaSessao.Entregas.Count == 0,
                "Nova sessao inicia sem dados anteriores");
            Console.WriteLine(verificacoes + " verificacoes aprovadas."); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
