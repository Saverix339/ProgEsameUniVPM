namespace ProgEsameUniVPM;

public static class GameManager
{
    public static Giocatore Giocatore { get; set; } = null!;
    public static IStato? StatoGioco;
    public static void Avanza()
    {
        
    }
    public static void CambiaStato(IStato Iniziale, IStato Cambio)
    {
        Iniziale.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }
}
