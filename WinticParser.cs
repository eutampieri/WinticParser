using System;
using System.Collections.Generic;

public class WinticLogParser
{
    private class WinticFilmData
    {
        public String PartitaIVA;
        public String TipoBiglietto;
        public String DataOraEmissioneRaw;
        public String TitoloFilm;
        public String OrarioProiezioneRaw;
        public String DataProiezioneRaw;
        public float Prezzo;
        public bool Annullato;
        public DateTime DataOraProiezione
        {
            get
            {
                int year = int.Parse(DataProiezioneRaw.Substring(0, 4));
                int month = int.Parse(DataProiezioneRaw.Substring(4, 2));
                int day = int.Parse(DataProiezioneRaw.Substring(6, 2));
                int hour = int.Parse(OrarioProiezioneRaw.Substring(0, 2));
                int minute = int.Parse(OrarioProiezioneRaw.Substring(2, 2));
                DateTime result = new DateTime(year, month, day, hour, minute, 0);
                return result;
            }
        }

        public DateTime DataOraEmissione
        {
            get
            {
                int year = int.Parse(DataOraEmissioneRaw.Substring(0, 4));
                int month = int.Parse(DataOraEmissioneRaw.Substring(4, 2));
                int day = int.Parse(DataOraEmissioneRaw.Substring(6, 2));
                int hour = int.Parse(DataOraEmissioneRaw.Substring(8, 2));
                int minute = int.Parse(DataOraEmissioneRaw.Substring(10, 2));
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
            DataProiezioneRaw = RawData.Substring(177, 8);
            Prezzo = float.Parse(RawData.Substring(261, 9)) / 100F;
        }

    }
    private readonly String WinticPath;
    private List<WinticFilmData> WinticLog;
    public struct WinticStats
    {
        public int Omaggi;
        public int Ridotti4;
        public int Ridotti5;
        public int Interi;
        public int Prevendite;
        public int IncassiPrecedenti;
    }
	public WinticLogParser(String path)
	{
        WinticPath = path;
        WinticLog = new List<WinticFilmData>();
	}
    private void LoadFile(DateTime date)
    {
        String filename = WinticPath + "\\logdir\\LOG_" + date.Year.ToString() +
            "_" + date.Month.ToString().PadLeft(2, '0') +
            "_" + date.Day.ToString().PadLeft(2, '0') + ".TXT";
        String[] file_rows;
        if (System.IO.File.Exists(filename)) {
            String file_content = System.IO.File.ReadAllText(@filename).Replace("\r", "");
            file_rows = file_content.Split('\n');
        } else
        {
            file_rows = new string[] { };
        }
        List<WinticFilmData> parsed = new List<WinticFilmData>();
        for(int i = 0; i < file_rows.Length - 1; i++)
        {
            parsed.Add(new WinticFilmData(file_rows[i]));
        }
        WinticLog.AddRange(parsed);
    }
    public WinticStats GetStats(DateTime Proiezione)
    {
        for(int i = 0; i <= 7; i++)
        {
            LoadFile(DateTime.Now.AddDays(-1 * i));
        }

        WinticStats result;
        result.Omaggi = 0;
        result.Ridotti4 = 0;
        result.Ridotti5 = 0;
        result.Interi = 0;
        result.Prevendite = 0;
        result.IncassiPrecedenti = 0;

        for(int i = 0; i < WinticLog.Count; i++)
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
                if (Proiezione - WinticLog[i].DataOraEmissione >= TimeSpan.FromMinutes(40))
                {
                    switch (WinticLog[i].TipoBiglietto)
                    {
                        case "IX":
                            result.IncassiPrecedenti += 7 * bigliettiVenduti;
                            break;
                        case "RA":
                            result.IncassiPrecedenti += 5 * bigliettiVenduti;
                            break;
                        case "RX":
                            result.IncassiPrecedenti += 4 * bigliettiVenduti;
                            break;
                        default:
                            break;
                    }
                }
            } else if(WinticLog[i].DataOraProiezione > Proiezione && WinticLog[i].DataOraEmissione - Proiezione < TimeSpan.FromMinutes(40)) {
                int bigliettiVenduti = WinticLog[i].Annullato ? -1 : 1;
                switch (WinticLog[i].TipoBiglietto)
                {
                    case "IX":
                        result.Prevendite += 7*bigliettiVenduti;
                        break;
                    case "RA":
                        result.Prevendite += 5*bigliettiVenduti;
                        break;
                    case "RX":
                        result.Prevendite += 4*bigliettiVenduti;
                        break;
                    default:
                        break;
                }
            }
        }

        return result;
    }
}
