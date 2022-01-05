using Optional;
using System;
using System.Collections.Generic;

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
        public struct WinticStats
        {
            public ISet<Seat> Omaggi;
            public ISet<Seat> Ridotti4;
            public ISet<Seat> Ridotti5;
            public ISet<Seat> Interi;
            public float Prevendite;
            public float IncassiPrecedenti;
            public ISet<Seat> PostiVenduti
            {
                get
                {
                    HashSet<Seat> result = new();
                    result.UnionWith(Omaggi);
                    result.UnionWith(Interi);
                    result.UnionWith(Ridotti5);
                    result.UnionWith(Ridotti4);
                    return result;
                }
            }
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
            result.Omaggi = new HashSet<Seat>();
            result.Ridotti4 = new HashSet<Seat>();
            result.Ridotti5 = new HashSet<Seat>();
            result.Interi = new HashSet<Seat>();
            result.Prevendite = 0;
            result.IncassiPrecedenti = 0;

            for (int i = 0; i < WinticLog.Count; i++)
            {
                if (WinticLog[i].DataOraProiezione == Proiezione)
                {
                    switch (WinticLog[i].TipoBiglietto)
                    {
                        case "OX":
                            if(WinticLog[i].Annullato)
                            {
                                result.Omaggi.Remove(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber=0}));
                            } else
                            {
                                result.Omaggi.Add(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }
                            break;
                        case "IX":
                            if (WinticLog[i].Annullato)
                            {
                                result.Interi.Remove(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }
                            else
                            {
                                result.Interi.Add(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }
                            break;
                        case "RA":
                            if (WinticLog[i].Annullato)
                            {
                                result.Ridotti5.Remove(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }
                            else
                            {
                                result.Ridotti5.Add(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }

                            break;
                        case "RX":
                            if (WinticLog[i].Annullato)
                            {
                                result.Ridotti4.Remove(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }
                            else
                            {
                                result.Ridotti4.Add(WinticLog[i].Posto.ValueOr(new Seat { Row = WinticLog[i].TipoBiglietto, SeatNumber = 0 }));
                            }

                            break;
                        default:
                            break;
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
            }

            return result;
        }
    }
}
