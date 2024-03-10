using Optional;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WinticParser
{
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
            public Option<Seat> Posto;
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
                    DateTime result = new(year, month, day, hour, minute, 0);
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
                    DateTime result = new(year, month, day, hour, minute, 0);
                    return result;
                }
            }


            public WinticFilmData(String RawData)
            {
                string[] RawSeat = RawData.Substring(47, 6).Trim().Split('/');
                Annullato = RawData[55] == 'A';
                PartitaIVA = RawData.Substring(0, 11);
                TipoBiglietto = RawData.Substring(53, 2);
                DataOraEmissioneRaw = RawData.Substring(64, 12);
                TitoloFilm = RawData.Substring(187, 40).Trim();
                OrarioProiezioneRaw = RawData.Substring(227, 4);
                DataProiezioneRaw = RawData.Substring(177, 8);
                Prezzo = float.Parse(RawData.Substring(261, 9)) / 100F;
                if (RawSeat.Length != 2)
                {
                    Posto = Option.None<Seat>();
                }
                else
                {
                    Posto = new Seat
                    {
                        Row = RawSeat[0],
                        SeatNumber = int.Parse(RawSeat[1])
                    }.Some();
                }
            }

        }
        private readonly String WinticPath;
        private readonly List<WinticFilmData> WinticLog;
        public struct Seat
        {
            public String Row;
            public int SeatNumber;
        }
        public struct PriceStatsRow
        {
            public int Price;
            public uint Quantity;

            public PriceStatsRow(int price, uint quantity) : this()
            {
                this.Price = price;
                this.Quantity = quantity;
            }
        }
        public struct WinticStats
        {
            public float Prevendite;
            public float IncassiPrecedenti;
            public IList<PriceStatsRow> PriceStats;
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
            if (System.IO.File.Exists(filename))
            {
                String file_content = System.IO.File.ReadAllText(@filename).Replace("\r", "");
                file_rows = file_content.Split('\n');
            }
            else
            {
                file_rows = Array.Empty<string>();
            }
            List<WinticFilmData> parsed = new();
            for (int i = 0; i < file_rows.Length - 1; i++)
            {
                parsed.Add(new WinticFilmData(file_rows[i]));
            }
            WinticLog.AddRange(parsed);
        }
        public WinticStats GetStats(DateTime Proiezione)
        {
            for (int i = 0; i <= 7; i++)
            {
                LoadFile(DateTime.Now.AddDays(-1 * i));
            }

            WinticStats result;
            result.Prevendite = 0F;
            result.IncassiPrecedenti = 0F;

            IDictionary<int, uint> priceStats = new Dictionary<int, uint>();

            for (int i = 0; i < WinticLog.Count; i++)
            {
                if (WinticLog[i].DataOraProiezione == Proiezione)
                {
                    int price = (int)(WinticLog[i].Prezzo * 100);
                    if (!priceStats.ContainsKey(price))
                    {
                        priceStats.Add(price, 0);
                    }
		            if(WinticLog[i].Annullato)
                    {
                        priceStats[price]--;
                    } else {
                        priceStats[price]++;
                    }
                    
                    if (Proiezione - WinticLog[i].DataOraEmissione >= TimeSpan.FromMinutes(60))
                    {
                        int bigliettiVenduti = WinticLog[i].Annullato ? -1 : 1;
                        result.IncassiPrecedenti += WinticLog[i].Prezzo * bigliettiVenduti;
                    }
                }
                else if (WinticLog[i].DataOraProiezione > Proiezione && Proiezione - WinticLog[i].DataOraEmissione < TimeSpan.FromMinutes(60))
                {
                    int bigliettiVenduti = WinticLog[i].Annullato ? -1 : 1;
                    result.IncassiPrecedenti += WinticLog[i].Prezzo * bigliettiVenduti;
                }
                else if (WinticLog[i].DataOraProiezione == Proiezione && Proiezione - WinticLog[i].DataOraEmissione >= TimeSpan.FromMinutes(60))
                {
                    int bigliettiVenduti = WinticLog[i].Annullato ? -1 : 1;
                    result.Prevendite += WinticLog[i].Prezzo * bigliettiVenduti;
                }
            }

            result.PriceStats = priceStats.Select(x => new PriceStatsRow(x.Key, x.Value)).ToList();

            return result;
        }
    }
}
