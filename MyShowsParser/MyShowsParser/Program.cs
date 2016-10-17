using System;
using HtmlAgilityPack;
using LiteDB;


namespace MyShowsParser
{
    public class ShowInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string OriginalName { get; set; }
        public string Country { get; set; }
        public string Genres { get; set; }
        public string Channel { get; set; }
        public string Whatchers { get; set; }
        public string AllDuration { get; set; }
        public string DurationOneSeries { get; set; }
        public string MyShowsRating { get; set; }
    }

    class Program
    {
        public const string NameDb = "ShowsDB.db";
        public const string NameCollection = "showsID";



        //поиск информации по ключу
        public static void GetShowInfo(string id)
        {
            ShowInfo show = new ShowInfo();
            string htmlShowId = "https://myshows.me/view/" + id + "/";
            HtmlDocument htmlDoc = null;
            HtmlWeb web = new HtmlWeb();
            htmlDoc = web.Load(htmlShowId);

            try
            {
                show.Id = id; 
                //Название сериала
                show.Name += "Сериал: " + htmlDoc.DocumentNode.SelectSingleNode("//main/h1[@itemprop='name']").InnerText.Trim(); ;
                //Оригинальное название
                show.OriginalName += "Оригинальное название: " + htmlDoc.DocumentNode.SelectSingleNode("//main/p[@class='subHeader']").InnerText.Trim();

                //информация из таблицы
                var info = htmlDoc.DocumentNode.SelectNodes(".//div[@class = 'clear']/p");
                foreach (var str in info)
                {
                    if (str.InnerText.Contains("Страна"))
                    {
                        show.Country = str.InnerText.Trim();
                        continue;
                    }
                    if (str.InnerText.Contains("Жанры"))
                    {
                        show.Genres = str.InnerText.Replace(" ", string.Empty).Replace("\n", " ");
                        continue;
                    }
                    if (str.InnerText.Contains("Канал"))
                    {
                        show.Channel = str.InnerText.Trim();
                        continue;
                    }
                    if (str.InnerText.Contains("Смотрящих"))
                    {
                        show.Whatchers = str.InnerText.Replace("&thinsp;", string.Empty);
                        continue;
                    }
                    if (str.InnerText.Contains("Общая длительность"))
                    {
                        show.AllDuration = str.InnerText.Trim();
                        continue;
                    }

                    if (str.InnerText.Contains("Длительность серии"))
                    {
                        show.DurationOneSeries = str.InnerText.Trim();
                        continue;
                    }
                    if (str.InnerText.Contains("Рейтинг MyShows"))
                        show.MyShowsRating = str.InnerText.Trim().Replace("\n", " ").Replace("&thinsp;", string.Empty);
                }
                AddShowInDB(show);
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

            HtmlDocument htmlDoc = null;
            HtmlWeb web = new HtmlWeb();
            htmlDoc = web.Load(htmlShowId);
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
            Console.WriteLine(show.Name);
            Console.WriteLine(show.OriginalName);
            Console.WriteLine(show.Country);
            Console.WriteLine(show.Genres);
            Console.WriteLine(show.Channel);
            Console.WriteLine(show.Whatchers);
            Console.WriteLine(show.AllDuration);
            Console.WriteLine(show.DurationOneSeries);
            Console.WriteLine(show.MyShowsRating);
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

        static void Main(string[] args)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("1.Поиск по ID сериала");
                Console.WriteLine("2.Поиск по ключевому слову");
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
                            break;

                        case 2:
                            Console.WriteLine("\nВведите ключевое слово поиска");
                            string word = Console.ReadLine();
                            Console.WriteLine("\nРезультат поиска по ключевому слову " + word + "\n");
                            string htmlShowWord = "https://myshows.me/search/?q=" + word;
                            id = GetShowId(htmlShowWord);
                            if (id == String.Empty)
                                continue;
                            break;
                        default:
                            Console.WriteLine("\nОшибка выбора действия!");
                            Console.WriteLine("Нажмите любую клавишу для продолжения");
                            Console.ReadLine();
                            continue;
                    }

                    ShowInfo searchRes = SearchInDB_ID(id);
                    if (searchRes == null) //если в кэше нет
                        GetShowInfo(id);
                    else
                    {
                        Console.WriteLine("(Информация из кэша)" + "\n");
                        PrintShowInfo(searchRes);
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
