using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using finalProject411.Models;
using Newtonsoft.Json.Linq;

namespace finalProject411
{
    class Program
    {
        static void Main(string[] args)
        {
            List<FlatCountry> data = importCountriesFromCsv();
            var datajson = importCountriesFromJson();
            List<Country> cleanjson = cleanDataJson(datajson);
            data = data.OrderBy(x => x.country).ToList();
            cleanjson = cleanjson.OrderBy(x => x.CountryData.Country).ToList();
            List<CombinationCountry> countries = new List<CombinationCountry>();
            foreach (var jsoncountry in cleanjson)
            {
                var datapiece = data.FirstOrDefault(x => x.country.Equals(jsoncountry.CountryData.Country));
                if (datapiece != null)
                {
                    countries.Add(new CombinationCountry()
                    {
                        acronym = datapiece.abbr,
                        country = datapiece.country,
                        females = datapiece.females,
                        government = datapiece.government,
                        institutions = datapiece.institutions,
                        juveniles = datapiece.juveniles,
                        occupancy = datapiece.occupancy,
                        population = datapiece.pop,
                        pretrial = datapiece.pretrial,
                        rate = datapiece.rate,
                        src = "csv"
                    });
                }
                else
                {
                    int a = 0;
                    bool rateSuccess = int.TryParse(jsoncountry.CountryData.Rate, out a);

                    if (rateSuccess)
                    {
                        countries.Add(new CombinationCountry()
                        {
                            acronym = jsoncountry.Abbreviation,
                            country = jsoncountry.CountryData.Country,
                            females = double.Parse(jsoncountry.CountryData.Females),
                            government = jsoncountry.CountryData.Government,
                            juveniles = double.Parse(jsoncountry.CountryData.Juveniles),
                            occupancy = double.Parse(jsoncountry.CountryData.Occupancy),
                            population = int.Parse(jsoncountry.CountryData.Pop),
                            rate = int.Parse(jsoncountry.CountryData.Rate),
                            src = "json"
                        });
                    }else
                    {
                        countries.Add(new CombinationCountry()
                        {
                            acronym = jsoncountry.Abbreviation,
                            country = jsoncountry.CountryData.Country,
                            females = double.Parse(jsoncountry.CountryData.Females),
                            government = jsoncountry.CountryData.Government,
                            juveniles = double.Parse(jsoncountry.CountryData.Juveniles),
                            occupancy = double.Parse(jsoncountry.CountryData.Occupancy),
                            population = int.Parse(jsoncountry.CountryData.Pop),
                            src = "json"
                        });
                    }
                }
            }


            var csvjson = new CsvWriter(new StreamWriter(@"..\..\Data\prisonData.csv"));
            csvjson.WriteRecords(cleanjson);//original data set for comparison
            var csv = new CsvWriter(new StreamWriter(@"..\..\Data\merged_data_final.csv"));
            csv.WriteRecords(countries);
            var csvForVisualization = new CsvWriter(new StreamWriter(@"..\..\..\javascript\merged_data_final.csv"));
            csvForVisualization.WriteRecords(countries);
        }

        private static List<Country> importCountriesFromJson()
        {
            List<Country> countries = new List<Country>();
 
            using (StreamReader r = new StreamReader(@"..\..\Data\prisonData.json"))
            {
                string data = r.ReadToEnd();
                var items = JsonConvert.DeserializeObject<Dictionary<string,CountryData>>(data);
                foreach (KeyValuePair<string, CountryData> country in items)
                {
                    countries.Add(new Country(country.Key, country.Value));
                }
            }
            return countries;
        }

        private static List<FlatCountry> importCountriesFromCsv()
        {
            var countries = new List<FlatCountry>();
            var reader = new StreamReader(@"..\..\Data\merged_dataframe_2.csv");
               var parser = new CsvReader(reader,  new CsvHelper.Configuration.Configuration
                {
                    HeaderValidated = null,
                    MissingFieldFound = null
                });
            countries.AddRange(parser.GetRecords<FlatCountry>().ToList());
            return countries;
        }
        private static List<Country> cleanDataJson(List<Country> countries)
        {
            List<Country> fullCountries = new List<Country>();
            var i = 0;
            foreach (Country country in countries)
            {
                if (country.CountryData.Females == "--" 
                    || country.CountryData.Juveniles == "--"
                    || country.CountryData.Occupancy == "--"
                    || country.CountryData.Pop == "--"
                    || country.CountryData.Rate == "--")
                {
                    i++;
                }
                else
                {
                    fullCountries.Add(country);
                }
            }
            Console.WriteLine(i + " unclean entries removed.");
            return fullCountries;
        }
    }
}
