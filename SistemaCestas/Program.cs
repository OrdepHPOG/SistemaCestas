using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SistemaCestas
{
    // Coleções do domínio são listas duplamente encadeadas (LinkedList<T>).
    // Os IDs mantêm as associações entre os objetos durante a sessão.
    internal static class Validacao
    {
        public static string Texto(string valor, string campo)
        {
            if (String.IsNullOrWhiteSpace(valor)) throw new ArgumentException(campo + " é obrigatório.");
            return valor.Trim();
        }
        public static void Positivo(int valor)
        {
            if (valor <= 0) throw new ArgumentException("A quantidade deve ser maior que zero.");
        }
    }

    public sealed class Produto
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; }
        public int Quantidade { get; private set; }
        public Produto(string nome, int quantidade)
        {
            Nome = Validacao.Texto(nome, "Nome do produto");
            Validacao.Positivo(quantidade);
            Id = Guid.NewGuid(); Quantidade = quantidade;
        }
        internal void Adicionar(int quantidade)
        {
            Validacao.Positivo(quantidade);
            Quantidade = checked(Quantidade + quantidade);
        }
        internal void Retirar(int quantidade)
        {
            Validacao.Positivo(quantidade);
            if (quantidade > Quantidade) throw new InvalidOperationException("Estoque insuficiente.");
            Quantidade -= quantidade;
        }
    }

    public sealed class ItemComposicao
    {
        public Guid ProdutoId { get; private set; }
        public int Quantidade { get; private set; }
        public ItemComposicao(Guid produtoId, int quantidade)
        {
            Validacao.Positivo(quantidade); ProdutoId = produtoId; Quantidade = quantidade;
        }
    }

    public sealed class Estoque
    {
        private LinkedList<Produto> produtos = new LinkedList<Produto>();
        private LinkedList<ItemComposicao> composicao = new LinkedList<ItemComposicao>();
        public IEnumerable<Produto> Produtos { get { foreach (var p in produtos) yield return p; } }
        public IEnumerable<ItemComposicao> Composicao { get { foreach (var i in composicao) yield return i; } }
        public bool ComposicaoDefinida { get { return composicao.Count > 0; } }
        public void CadastrarProduto(string nome, int quantidade)
        {
            nome = Validacao.Texto(nome, "Nome"); Validacao.Positivo(quantidade);
            var existente = produtos.FirstOrDefault(p => String.Equals(p.Nome, nome, StringComparison.OrdinalIgnoreCase));
            if (existente == null) produtos.AddLast(new Produto(nome, quantidade));
            else existente.Adicionar(quantidade);
        }
        public Produto ObterProduto(Guid id)
        {
            var produto = produtos.FirstOrDefault(p => p.Id == id);
            if (produto == null) throw new InvalidOperationException("Produto não encontrado.");
            return produto;
        }
        public void RetirarProduto(Guid id, int quantidade) { ObterProduto(id).Retirar(quantidade); }
        // RD02: a organização informa a composição, sem uma receita presumida.
        public void DefinirComposicao(LinkedList<ItemComposicao> itens)
        {
            if (itens == null || itens.Count == 0) throw new ArgumentException("Informe ao menos um produto.");
            var nova = new LinkedList<ItemComposicao>();
            foreach (var item in itens)
            {
                ObterProduto(item.ProdutoId); Validacao.Positivo(item.Quantidade);
                if (nova.Any(i => i.ProdutoId == item.ProdutoId)) throw new ArgumentException("Produto repetido na composição.");
                nova.AddLast(new ItemComposicao(item.ProdutoId, item.Quantidade));
            }
            composicao = nova;
        }
        public int CalcularCestas()
        {
            if (!ComposicaoDefinida) return 0;
            int resultado = Int32.MaxValue;
            foreach (var item in composicao)
                resultado = Math.Min(resultado, ObterProduto(item.ProdutoId).Quantidade / item.Quantidade);
            return resultado;
        }
        internal void BaixarUmaCesta()
        {
            if (!ComposicaoDefinida) throw new InvalidOperationException("A organização ainda não definiu a composição da cesta.");
            if (CalcularCestas() < 1) throw new InvalidOperationException("Não há produtos suficientes para uma cesta.");
            foreach (var item in composicao) RetirarProduto(item.ProdutoId, item.Quantidade);
        }
    }

    public sealed class Familias
    {
        public Guid Id { get; private set; }
        public string Nome { get; private set; } // RF03 inclui nome.
        public string Sobrenome { get; private set; }
        public string Endereco { get; private set; }
        public bool Autorizada { get; private set; }
        public Familias(string nome, string sobrenome, string endereco)
        {
            Id = Guid.NewGuid(); Nome = Validacao.Texto(nome, "Nome");
            Sobrenome = Validacao.Texto(sobrenome, "Sobrenome");
            Endereco = Validacao.Texto(endereco, "Endereço"); Autorizada = false;
        }
        internal void DefinirAutorizacao(bool autorizada) { Autorizada = autorizada; }
    }

    public sealed class Visita
    {
        public Guid FamiliaId { get; private set; }
        public DateTime Data { get; private set; }
        public Visita(Guid familiaId, DateTime data) { FamiliaId = familiaId; Data = data.Date; }
    }

    public sealed class Entrega
    {
        public Guid FamiliaId { get; private set; }
        public DateTime Data { get; private set; }
        private Entrega(Guid familiaId, DateTime data) { FamiliaId = familiaId; Data = data.Date; }
        // Uma entrega representa a retirada de UMA cesta; apenas data, sem horário.
        internal static void Registrar(LinkedList<Entrega> entregas, Familias familia, DateTime data)
        {
            entregas.AddLast(new Entrega(familia.Id, data));
        }
    }

    public sealed class Cestas
    {
        public void RetirarCesta(Estoque estoque, Familias familia, DateTime data, LinkedList<Entrega> entregas)
        {
            if (!familia.Autorizada) throw new InvalidOperationException("Família não autorizada a receber cesta.");
            estoque.BaixarUmaCesta();
            Entrega.Registrar(entregas, familia, data);
        }
    }

    public sealed class Adm
    {
        public string Nome { get; private set; }
        private byte[] salt;
        private byte[] senhaHash;
        private const int Iteracoes = 150000;
        public Adm(string nome, string senha)
        {
            Nome = Validacao.Texto(nome, "Nome do administrador");
            if (String.IsNullOrWhiteSpace(senha)) throw new ArgumentException("Senha obrigatória.");
            salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            senhaHash = Hash(senha);
        }
        private byte[] Hash(string senha)
        {
            using (var derivador = new Rfc2898DeriveBytes(senha, salt, Iteracoes, HashAlgorithmName.SHA256))
                return derivador.GetBytes(32);
        }
        public bool Autenticar(string nome, string senha)
        {
            byte[] tentativa = Hash(senha);
            int diferenca = 0;
            for (int i = 0; i < senhaHash.Length; i++) diferenca |= senhaHash[i] ^ tentativa[i];
            return diferenca == 0 && String.Equals(Nome, nome, StringComparison.Ordinal);
        }
        public void CadastrarProduto(Estoque estoque, string nome, int quantidade) { estoque.CadastrarProduto(nome, quantidade); }
        public void CadastrarFamilia(LinkedList<Familias> familias, string nome, string sobrenome, string endereco)
        { familias.AddLast(new Familias(nome, sobrenome, endereco)); }
        public void AgendarVisita(LinkedList<Visita> visitas, Familias familia, DateTime data)
        { visitas.AddLast(new Visita(familia.Id, data)); }
        public void AutorizarFamilia(Familias familia, bool autorizada) { familia.DefinirAutorizacao(autorizada); }
    }

    public sealed class DadosSistema
    {
        private readonly LinkedList<Adm> administradores = new LinkedList<Adm>();
        public Adm Administrador { get { return administradores.First.Value; } }
        public Estoque Estoque { get; private set; }
        internal LinkedList<Familias> Familias { get; private set; }
        internal LinkedList<Visita> Visitas { get; private set; }
        internal LinkedList<Entrega> Entregas { get; private set; }
        public DadosSistema(Adm administrador)
        {
            if (administrador == null) throw new ArgumentNullException("administrador");
            administradores.AddLast(administrador); Estoque = new Estoque();
            Familias = new LinkedList<Familias>(); Visitas = new LinkedList<Visita>(); Entregas = new LinkedList<Entrega>();
        }
        internal Familias ObterFamilia(Guid id)
        {
            var familia = Familias.FirstOrDefault(f => f.Id == id);
            if (familia == null) throw new InvalidOperationException("Família não encontrada.");
            return familia;
        }
    }

    internal static class Program
    {
        private static DadosSistema dados;
        public static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            try
            {
                Console.WriteLine("SISTEMA DE CESTAS BÁSICAS");
                Console.WriteLine("Dados somente em memória: ao encerrar, todos os cadastros serão perdidos.");
                RegistrarConta();
                Menu();
                return 0;
            }
            catch (EndOfStreamException) { return 0; }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Não foi possível continuar: " + ex.Message);
                return 1;
            }
        }
        private static void RegistrarConta()
        {
            Console.WriteLine("REGISTRO DE CONTA DO ADMINISTRADOR");
            while (true)
            {
                string nome = Texto("Nome do administrador: ");
                string senha = Senha("Crie uma senha: ");
                if (String.IsNullOrWhiteSpace(senha))
                {
                    Console.WriteLine("A senha é obrigatória. Tente novamente.");
                    continue;
                }
                if (senha != Senha("Confirme a senha: "))
                {
                    Console.WriteLine("As senhas não coincidem. Tente novamente.");
                    continue;
                }
                dados = new DadosSistema(new Adm(nome, senha));
                Console.WriteLine("Conta registrada nesta sessão. Bem-vindo, " + dados.Administrador.Nome + "!");
                return; // O cadastro dá acesso direto ao menu, sem pedir login.
            }
        }
        private static void Alterar(Action<DadosSistema> acao)
        {
            acao(dados);
            Console.WriteLine("Operação concluída. Dados mantidos somente em memória.");
        }
        private static void Menu()
        {
            while (true)
            {
                Console.WriteLine("\n1 - Cadastrar produto / adicionar quantidade\n2 - Retirar produto\n3 - Consultar estoque e quantidade de cestas\n4 - Informar composição da cesta (RD02)\n5 - Cadastrar família\n6 - Consultar famílias e atendimento\n7 - Agendar visita\n8 - Consultar visitas\n9 - Definir autorização da família\n10 - Registrar entrega de uma cesta\n11 - Retirar uma cesta\n12 - Consultar entregas/retiradas\n0 - Sair");
                string opcao = Texto("Opção: ");
                try
                {
                    switch (opcao)
                    {
                        case "0": return;
                        case "1":
                            string nome = Texto("Nome do produto: "); int quantidade = Inteiro("Quantidade a adicionar: ");
                            Alterar(d => d.Administrador.CadastrarProduto(d.Estoque, nome, quantidade)); break;
                        case "2":
                            Guid produto = SelecionarProduto(); int retirada = Inteiro("Quantidade a retirar: ");
                            Alterar(d => d.Estoque.RetirarProduto(produto, retirada)); break;
                        case "3": MostrarEstoque(); break;
                        case "4": ConfigurarComposicao(); break;
                        case "5":
                            string familiaNome = Texto("Nome: "); string sobrenome = Texto("Sobrenome: "); string endereco = Texto("Endereço: ");
                            Alterar(d => d.Administrador.CadastrarFamilia(d.Familias, familiaNome, sobrenome, endereco)); break;
                        case "6": MostrarFamilias(); break;
                        case "7":
                            Guid familiaVisita = SelecionarFamilia(); DateTime visita = Data("Data da visita (dd/MM/aaaa): ");
                            Alterar(d => d.Administrador.AgendarVisita(d.Visitas, d.ObterFamilia(familiaVisita), visita)); break;
                        case "8":
                            if (dados.Visitas.Count == 0) Console.WriteLine("Nenhuma visita agendada.");
                            foreach (var v in dados.Visitas) Console.WriteLine(NomeFamilia(v.FamiliaId) + " | " + v.Data.ToString("dd/MM/yyyy"));
                            break;
                        case "9":
                            Guid familiaAutorizacao = SelecionarFamilia(); bool autorizada = SimNao("Autorizar recebimento? (s/n): ");
                            Alterar(d => d.Administrador.AutorizarFamilia(d.ObterFamilia(familiaAutorizacao), autorizada)); break;
                        case "10": case "11":
                            Guid familiaEntrega = SelecionarFamilia(); DateTime entrega = Data("Data da entrega/retirada (dd/MM/aaaa): ");
                            Console.WriteLine("Será registrada UMA cesta, com baixa dos produtos. Use somente uma das opções 10/11 para o mesmo atendimento.");
                            Alterar(d => new Cestas().RetirarCesta(d.Estoque, d.ObterFamilia(familiaEntrega), entrega, d.Entregas)); break;
                        case "12":
                            if (dados.Entregas.Count == 0) Console.WriteLine("Nenhuma entrega registrada.");
                            foreach (var e in dados.Entregas) Console.WriteLine(NomeFamilia(e.FamiliaId) + " | " + e.Data.ToString("dd/MM/yyyy") + " | 1 cesta");
                            break;
                        default: Console.WriteLine("Opção inválida."); break;
                    }
                }
                catch (EndOfStreamException) { throw; }
                catch (Exception ex) { Console.WriteLine("Operação não concluída: " + ex.Message); }
            }
        }
        private static string NomeFamilia(Guid id)
        { var f = dados.ObterFamilia(id); return f.Nome + " " + f.Sobrenome + " | " + f.Endereco; }
        private static void MostrarFamilias()
        {
            if (dados.Familias.Count == 0) Console.WriteLine("Nenhuma família cadastrada.");
            int numero = 1;
            foreach (var f in dados.Familias)
                Console.WriteLine((numero++) + " - " + NomeFamilia(f.Id) + " | Autorizada: " + (f.Autorizada ? "sim" : "não") + " | Cestas recebidas: " + dados.Entregas.Count(e => e.FamiliaId == f.Id));
        }
        private static void MostrarEstoque()
        {
            if (!dados.Estoque.Produtos.Any()) Console.WriteLine("Nenhum produto cadastrado.");
            int numero = 1;
            foreach (var p in dados.Estoque.Produtos) Console.WriteLine((numero++) + " - " + p.Nome + " | Quantidade: " + p.Quantidade);
            Console.WriteLine(dados.Estoque.ComposicaoDefinida ? "Cestas disponíveis: " + dados.Estoque.CalcularCestas() : "Composição não definida; informe-a na opção 4 para calcular cestas.");
            foreach (var i in dados.Estoque.Composicao) Console.WriteLine("Por cesta: " + dados.Estoque.ObterProduto(i.ProdutoId).Nome + " = " + i.Quantidade);
        }
        private static Guid SelecionarProduto()
        {
            if (!dados.Estoque.Produtos.Any()) throw new InvalidOperationException("Cadastre um produto primeiro.");
            MostrarEstoque(); int indice = Inteiro("Número do produto: "); int atual = 1;
            foreach (var p in dados.Estoque.Produtos) if (atual++ == indice) return p.Id;
            throw new ArgumentException("Produto inválido.");
        }
        private static Guid SelecionarFamilia()
        {
            if (dados.Familias.Count == 0) throw new InvalidOperationException("Cadastre uma família primeiro.");
            MostrarFamilias(); int indice = Inteiro("Número da família: "); int atual = 1;
            foreach (var f in dados.Familias) if (atual++ == indice) return f.Id;
            throw new ArgumentException("Família inválida.");
        }
        private static void ConfigurarComposicao()
        {
            Console.WriteLine("Informe a composição completa determinada pela organização. Ela substituirá a composição anterior.");
            var itens = new LinkedList<ItemComposicao>();
            do
            {
                Guid id = SelecionarProduto(); int quantidade = Inteiro("Quantidade deste produto em UMA cesta: ");
                if (itens.Any(i => i.ProdutoId == id)) throw new ArgumentException("Produto já informado; composição anterior preservada.");
                itens.AddLast(new ItemComposicao(id, quantidade));
            } while (SimNao("Incluir outro produto? (s/n): "));
            Alterar(d => d.Estoque.DefinirComposicao(itens));
        }
        private static string Texto(string mensagem)
        {
            while (true)
            {
                Console.Write(mensagem); string entrada = Console.ReadLine();
                if (entrada == null) throw new EndOfStreamException();
                if (!String.IsNullOrWhiteSpace(entrada)) return entrada.Trim();
                Console.WriteLine("Preencha o campo.");
            }
        }
        private static int Inteiro(string mensagem)
        {
            int numero;
            while (!Int32.TryParse(Texto(mensagem), out numero) || numero <= 0) Console.WriteLine("Informe um inteiro maior que zero.");
            return numero;
        }
        private static DateTime Data(string mensagem)
        {
            DateTime data;
            while (!DateTime.TryParseExact(Texto(mensagem), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out data))
                Console.WriteLine("Data inválida. Use dd/MM/aaaa.");
            return data.Date;
        }
        private static bool SimNao(string mensagem)
        {
            while (true)
            {
                string resposta = Texto(mensagem).ToLowerInvariant();
                if (resposta == "s") return true;
                if (resposta == "n") return false;
                Console.WriteLine("Digite s ou n.");
            }
        }
        private static string Senha(string mensagem)
        {
            Console.Write(mensagem);
            if (Console.IsInputRedirected)
            {
                string entrada = Console.ReadLine();
                if (entrada == null) throw new EndOfStreamException();
                return entrada;
            }
            var senha = new StringBuilder();
            while (true)
            {
                var tecla = Console.ReadKey(true);
                if (tecla.Key == ConsoleKey.Enter) { Console.WriteLine(); return senha.ToString(); }
                if (tecla.Key == ConsoleKey.Backspace) { if (senha.Length > 0) senha.Length--; }
                else if (!Char.IsControl(tecla.KeyChar)) senha.Append(tecla.KeyChar);
            }
        }
    }
}
