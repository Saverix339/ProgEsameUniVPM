namespace ProgEsameUniVPM;

public static class GameManager
{
    public static Giocatore Giocatore { get; set; } = null!;
    public static IStato? StatoGioco;
    public static void Avanza()
    {
        
    }
    public static void CambiaStato(IStato Cambio)
    {
        StatoGioco?.esci();
        StatoGioco = Cambio;
        StatoGioco.entra();
    }
}
