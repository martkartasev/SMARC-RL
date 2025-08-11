using System.Globalization;
using System.IO;
using CsvHelper;
using UnityEngine;

namespace BagReplay
{
    public class BagDataWriter : MonoBehaviour
    {
        public BagReplay replay;

        public void Start()
        {
            using (var obsStreamWriter = new StreamWriter(Application.dataPath + "\\file.csv"))
            using (var obsWriter = new CsvWriter(obsStreamWriter, CultureInfo.InvariantCulture))
            {
                obsWriter.Context.RegisterClassMap<BagCsvRowMap>();
                obsWriter.WriteHeader<BagCsvRow>();
                obsWriter.NextRecord();

                var valueTuple = replay.GetStartEnd();
                var start = valueTuple.Item1 / 1000000000f;
                var currentTime = replay.startOffset * 1000000000 + start;

                var bagRow = replay.ReadFields(currentTime);

              
                    obsWriter.WriteRecord(bagRow.ToCsv());
                    obsWriter.NextRecord();
                    
                    currentTime += Time.fixedDeltaTime;
                    bagRow = replay.ReadFields(currentTime);
            }
        }
    }
}