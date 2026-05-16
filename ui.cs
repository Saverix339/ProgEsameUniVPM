using System;
namespace ProgEsameUniVPM;
public static class UI
{
    /* private enum StatoInit {
        ScriviNome,
        PrendiArma,
        Conferma,
        FineInit
    }
    private static StatoInit stato; */
    public static void MostraInizioPersonaggio()
    {
        Console.WriteLine("Inserisci il nome del tuo personaggio: \n");
        while(true){
            string nome = Console.ReadLine() ?? "";
            if((nome == "") || (nome.Length > 16))
            {
                Console.WriteLine("Nome invalido.");
            }
            else
            {
                Giocatore.InserimentoNome(nome);
            }
        }
    }

    public static void MostraStanza(Stanza s)
    {
        Console.WriteLine($"===={s.Nome.ToUpper()}====");
        Console.WriteLine($"Piano: {s.Livello}");
        Console.WriteLine($"{s.Descrizione}");
        Console.WriteLine($"Azioni (digita per eseguire): " + string.Join("\n", s.Azioni.Keys));
    }

    public static string Input()
    {
        Console.WriteLine($"\n{Giocatore.Nome}>");
        return Console.ReadLine()?.Trim().ToLower() ?? "";
    }

}