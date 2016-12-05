using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using LiteDB;


namespace MyShowsParser
{
    class Program
    {
        public const string NameDb = "ShowsDB.db";
        public const string NameCollection = "showsID";

        //поиск информации по ключу
        public static void GetShowInfo(string id)
        {
            ShowInfo show = new ShowInfo();
            string htmlShowId = "https://myshows.me/view/" + id + "/";
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(htmlShowId);

            try
            {
                show.Id = id; 
                //Название сериала
                show.Name += htmlDoc.DocumentNode.SelectSingleNode("//main/h1[@itemprop='name']").InnerText.Trim(); ;
                //Оригинальное название
                show.OriginalName +=  htmlDoc.DocumentNode.SelectSingleNode("//main/p[@class='subHeader']").InnerText.Trim();

                //информация из таблицы
                var info = htmlDoc.DocumentNode.SelectNodes(".//div[@class = 'clear']/p");
                foreach (var str in info)
                {
                    if (str.InnerText.Contains("Страна"))
                        show.Country = str.InnerText.Trim().Substring(8);

                    else if (str.InnerText.Contains("Жанры"))
                        show.Genres = str.InnerText.Replace(" ", string.Empty).Replace("\n", " ").Substring(7);

                    else if (str.InnerText.Contains("Канал"))
                        show.Channel = str.InnerText.Trim().Substring(7);

                    else if (str.InnerText.Contains("Смотрящих"))
                        show.Whatchers = str.InnerText.Replace("&thinsp;", string.Empty).Substring(11);

                    else if (str.InnerText.Contains("Общая длительность"))
                        show.AllDuration = str.InnerText.Trim().Substring(20);

                    else if (str.InnerText.Contains("Длительность серии"))
                        show.DurationOneSeries = str.InnerText.Trim().Substring(20);

                    else if (str.InnerText.Contains("Рейтинг MyShows"))
                        show.MyShowsRating =
                            str.InnerText.Trim().Replace("\n", " ").Replace("&thinsp;", string.Empty).Substring(17);
                }
                AddShowInDB(show);
                AddShowInDb_Entity(show);
                PrintShowInfo(show);
            }
            catch (Exception)
            {
                Console.WriteLine("\nНеверный ключ");
                Console.WriteLine("Нажмите Enter для продолжения");
                Console.ReadLine();
            }

        }

        //возвращает ид сериала при поиске по слову 
        public static string GetShowId(string htmlShowId)
        {           
            HtmlWeb web = new HtmlWeb();
            HtmlDocument htmlDoc = web.Load(htmlShowId);
            try
            {
                //ссылка на первый найденный сериал
                string link =
                    htmlDoc.DocumentNode.SelectSingleNode("//main/table[@class='catalogTable']/tr/td/a").Attributes[0]
                        .Value.Substring(24);// выделить ид
                return link.Remove(link.Length-1);//удалить символ / в конце
            }
            catch (Exception)
            {
                Console.WriteLine("\nПо данному запросу ничего не найдено!");
                Console.WriteLine("Нажмите Enter для продолжения");
                Console.ReadLine();
                return String.Empty;
            }
        }

        //вывод информации о сериале
        private static void PrintShowInfo(ShowInfo show)
        {
            Console.WriteLine("Сериал: " + show.Name);
            Console.WriteLine("Оригинальное название: " + show.OriginalName);
            Console.WriteLine("Страна: " + show.Country);
            Console.WriteLine("Жанры: " + show.Genres);
            Console.WriteLine("Канал: " + show.Channel);
            Console.WriteLine("Смотрящих: " + show.Whatchers);
            Console.WriteLine("Общая длительность: " + show.AllDuration);
            Console.WriteLine("Длительность одной серии: " + show.DurationOneSeries);
            Console.WriteLine("Рейтинг MyShows: " + show.MyShowsRating);
            Console.ReadLine();
        }

        //добавление в кэш
        public static void AddShowInDB(ShowInfo show)
        {
            // открывает базу данных, если ее нет - то создает
            using (var db = new LiteDatabase(NameDb))
            {
                // Получаем коллекцию
                var collectionShows = db.GetCollection<ShowInfo>(NameCollection);
                //добавляем новый элемент
                collectionShows.Insert(show);
            }
        }

        //поиск в кэше
        public static ShowInfo SearchInDB_ID(string ID)
        {
            // открывает базу данных, если ее нет - то создает
            using (var db = new LiteDatabase(NameDb))
            {
                // Получаем коллекцию
                var collectionShows = db.GetCollection<ShowInfo>(NameCollection);
                var resultSearch = collectionShows.FindOne(x => x.Id.Equals(ID));
                return resultSearch;
            }
        }

        //добавить в базу данных
        public static void AddShowInDb_Entity(ShowInfo show)
        {
            using (var db = new Context())
            {
                var country = db.Countries.Find(show.Country);
                if (country == null)
                {
                    country = new CountryModel()
                    {
                        Name = show.Country
                    };
                }

                var new_show = new ShowModel()
                {
                    Name = show.Name,
                    OriginalName = show.OriginalName,
                    Country = country,
                    Genres = show.Genres,
                    MyShowsRating = show.MyShowsRating
                };

                db.Shows.Add(new_show);
                db.SaveChanges();
            }
        }

        //поиск всех фильмов одного автора
        public static List<ShowModel> GetShowsByCountry(string country)
        {
            using (var db = new Context())
            {
                return
                   db.Shows.Where(x => x.Country.Name.ToLower() == country.ToLower()).ToList();
            }
        }

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("1.Поиск по ID сериала");
                Console.WriteLine("2.Поиск по ключевому слову");
                Console.WriteLine("3.Поиск фильмов по странам");
                try
                {
                    int choice = int.Parse(Console.ReadLine());
                    string id = "";
                    switch (choice)
                    {
                        case 1:

                            Console.WriteLine("\nВведите ID сериала");
                            id = Console.ReadLine();
                            Console.WriteLine("\nРезультат поиска по ID сериала " + id + "\n");
                            ShowInfo searchRes = SearchInDB_ID(id);
                            if (searchRes == null) //если в кэше нет
                                GetShowInfo(id);
                            else
                            {
                                Console.WriteLine("(Информация из кэша)" + "\n");
                                PrintShowInfo(searchRes);
                            }
                            break;

                        case 2:
                            Console.WriteLine("\nВведите ключевое слово поиска");
                            string word = Console.ReadLine();
                            Console.WriteLine("\nРезультат поиска по ключевому слову " + word + "\n");
                            string htmlShowWord = "https://myshows.me/search/?q=" + word;
                            id = GetShowId(htmlShowWord);
                            if (id == String.Empty)
                                continue;
                            searchRes = SearchInDB_ID(id);
                            if (searchRes == null) //если в кэше нет
                                GetShowInfo(id);
                            else
                            {
                                Console.WriteLine("(Информация из кэша)" + "\n");
                                PrintShowInfo(searchRes);
                            }
                            break;
                        case 3:
                            Console.WriteLine("\nВведите название страны");
                            var country = Console.ReadLine();

                            var list = GetShowsByCountry(country);
                            if (list.Count == 0)
                            {
                                Console.WriteLine("Нет данных о сериалах, снятых в выбранной стране!");
                                Console.ReadLine();
                                break;
                            }
                            Console.WriteLine("\nСериалы, снятые в " + country + ":\n");
                            foreach (var show in list)
                            {
                                Console.WriteLine("Название: "+show.Name);
                                Console.WriteLine("Оригинальное название: " + show.OriginalName);
                                Console.WriteLine("Жанры: " + show.Genres);
                                Console.WriteLine("Рейтинг MyShows: " + show.MyShowsRating);
                                Console.WriteLine("\n");
                            }
                            Console.ReadLine();
                            break;
                        default:
                            Console.WriteLine("\nОшибка выбора действия!");
                            Console.WriteLine("Нажмите любую клавишу для продолжения");
                            Console.ReadLine();
                            continue;
                    }


                }
                catch (Exception)
                {
                    Console.WriteLine("Ошибка выбора действия!");
                    Console.WriteLine("Нажмите Enter для продолжения");
                    Console.ReadLine();
                }
            }
        }
    }
}
