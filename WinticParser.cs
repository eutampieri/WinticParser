using System;

public class WinticLogParser
{
    private class WinticFilmData
    {
        public String PartitaIVA;
        public String TipoBiglietto;
        public String DataOraEmissioneRaw;
        public String TitoloFilm;
        public String OrarioProiezioneRaw;
        public float Prezzo;
        public bool Annullato;
        public DateTime DataOraProiezione
        {
            get
            {
                int year = int.Parse(DataOraEmissioneRaw.Substring(0, 4));
                int month = int.Parse(DataOraEmissioneRaw.Substring(4, 2));
                int day = int.Parse(DataOraEmissioneRaw.Substring(6, 2));
                int hour = int.Parse(OrarioProiezioneRaw.Substring(0, 2));
                int minute = int.Parse(OrarioProiezioneRaw.Substring(2, 2));
                DateTime result = new DateTime(year, month, day, hour, minute, 0);
                return result;
            }
        }

        public WinticFilmData(String RawData)
        {
            Annullato = RawData[55] == 'A';
            PartitaIVA = RawData.Substring(0, 11);
            TipoBiglietto = RawData.Substring(53, 2);
            DataOraEmissioneRaw = RawData.Substring(64, 12);
            TitoloFilm = RawData.Substring(187, 40).Trim();
            OrarioProiezioneRaw = RawData.Substring(227, 4);
            Prezzo = float.Parse(RawData.Substring(261, 9)) / 100F;
        }

    }
    private readonly String WinticPath;
    private WinticFilmData[] WinticLog;
    public struct WinticStats
    {
        public int Omaggi;
        public int Ridotti4;
        public int Ridotti5;
        public int Interi;
    }
	public WinticLogParser(String path)
	{
        WinticPath = path;
	}
    private void LoadFile()
    {
        DateTime now = DateTime.Now;
        String filename = WinticPath + "\\logdir\\LOG_" + now.Year.ToString() +
            "_" + now.Month.ToString().PadLeft(2, '0') +
            "_" + now.Day.ToString().PadLeft(2, '0') + ".TXT";
        String file_content = System.IO.File.ReadAllText(@filename).Replace("\r", "");
        String[] file_rows = file_content.Split('\n');
        System.Collections.Generic.List<WinticFilmData> parsed = new System.Collections.Generic.List<WinticFilmData>();
        for(int i = 0; i < file_rows.Length - 1; i++)
        {
            parsed.Add(new WinticFilmData(file_rows[i]));
        }
        WinticLog = parsed.ToArray();
    }
    public WinticStats GetStats(DateTime Proiezione)
    {
        LoadFile();

        WinticStats result;
        result.Omaggi = 0;
        result.Ridotti4 = 0;
        result.Ridotti5 = 0;
        result.Interi = 0;

        for(int i = 0; i < WinticLog.Length; i++)
        {
            if(WinticLog[i].DataOraProiezione == Proiezione)
            {
                int bigliettiVenduti = WinticLog[i].Annullato ? -1 : 1;
                switch (WinticLog[i].TipoBiglietto)
                {
                    case "OX":
                        result.Omaggi += bigliettiVenduti;
                        break;
                    case "IX":
                        result.Interi += bigliettiVenduti;
                        break;
                    case "RA":
                        result.Ridotti5 += bigliettiVenduti;
                        break;
                    case "RX":
                        result.Ridotti4 += bigliettiVenduti;
                        break;
                    default:
                        break;
                }
            }
        }

        return result;
    }
}
