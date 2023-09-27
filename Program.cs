// See https://aka.ms/new-console-template for more information
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Io;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Закачка данных...");
string test_url = "https://hh.ru/search/vacancy?search_field=name&search_field=company_name&search_field=description&enable_snippets=false&L_save_area=true&employment=full&schedule=remote&text=%D1%80%D0%B0%D0%B7%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D1%87%D0%B8%D0%BA+C%23&page=*PAGE*";

ParseHH parser = new ParseHH(test_url,1,40,650);

System.IO.File.WriteAllText("out.csv", "Name;Salary min;Salary max;Link;Function;Address\r\n");

foreach (Proffi proffi in parser.proffi_list)
{
    Console.WriteLine($" {proffi.Name_Formatted ,-50} │ {proffi.GetMin,7} - {proffi.GetMax,7} │  {proffi.Function,13}│ {proffi.LinkHref} │ {proffi.Address,-50}");
    System.IO.File.AppendAllText("out.csv", $"{proffi.Name};{proffi.GetMin};{proffi.GetMax};{proffi.LinkHref};{proffi.Function};{proffi.Address}\r\n");
}



/// <summary>
/// Класс для обработки сайта HH.ru
/// </summary>
public class ParseHH
{

    public List<Proffi> proffi_list = null;
    public List <IElement> AllLinks = null;
    private int TimeOut = 300;

    /// <summary>
    /// Конструктор
    /// </summary>
    /// <param name="url">Адрес страницы</param>
    /// <param name="min_page">Номер страницы *PAGE* в Url</param>
    /// <param name="max_page">Конечный номер страницы</param>
    /// <param name="pause">Сетевая задержка миллисекундах. Слишком быстро - все тупит</param>
    public ParseHH(string url, int min_page, int max_page, int pause=300)
    {
        TimeOut = pause;
        AllLinks = new List<IElement>();
        proffi_list = new List<Proffi>();

        bool Update_AllLinks = false;

        int Loading = 0;

        Console.Write("Закачка: ");

        const int ERROR = -1;
        const int OK = 1;

        for (int ix = min_page; ix < max_page; ix++)
        {
            Task T = new Task(async () =>
            {
                int i = ix;

                 Loading++;
                 using (Site site = new Site(url.Replace("*PAGE*", i.ToString()), "vacancy-serp-item-body",TimeOut))
                 {
                    if (site == null) return ;

                    while (Update_AllLinks)
                            { Thread.Sleep(20); }
                    
                    Update_AllLinks=true;
                    if (site != null)
                        try { AllLinks.AddRange(site.GetAllLinks.AsParallel()); } catch { return ; ; }
                    Update_AllLinks = false;



                    /* foreach (var link in site.GetAllLinks)
                     {
                         Console.WriteLine($" {link.TextContent}-> {link.Attributes["href"].Value}");
                     }    */

                    //ФИльтр


                    foreach (IElement item in site.GetAllClasses)
                     {
                         Proffi proffi = new Proffi("");

                         foreach (var span in item.GetElementsByTagName("span"))
                         {
                            // serp-item__title - Name 
                            // bloko-header-section-2  - ЗП
                            if (span.ClassName == null)
                            {
                                proffi.Name = span.TextContent;
                                try 
                                { 
                                    proffi.LinkHref = span.ChildNodes.GetElementsByTagName("a").First().GetAttribute("href").ToString();
                                    proffi.LinkHref = proffi.LinkHref.Substring(0, proffi.LinkHref.IndexOf("?"));
                                } catch { }
                            }
                             if (span.ClassName == "bloko-header-section-2")
                                 proffi._Salary = span.TextContent.ToLower().Replace(" ", "").Replace("от", "").Replace("до", "").Replace("?", "");

                         }//foreac
                          //
                         proffi.Address = item.GetElementsByClassName("bloko-text").First().TextContent;

                         // g-user-content  - функции
                         //proffi._Function = item.GetElementsByClassName("g-user-content").First().TextContent;

                         proffi_list.Add(proffi);

                     }
                     //чистим дубли
                     
                 }
            });

            T.ContinueWith(t => { Console.Write("x"); Loading--; });
            T.Start();
            Thread.Sleep(TimeOut);
        }


        Console.WriteLine("");
        Console.Write("Доделываем:");

        do
        {
            Console.Write(".");
            Thread.Sleep(TimeOut*2);
        } while (Loading > 1) ;

        AllLinks = AllLinks.Distinct().ToList();
        proffi_list = proffi_list.Distinct().ToList();

        Console.WriteLine("готово!");
    } //ParseHH

}



public class Proffi
{
    private string _Name = "";
    private string _Address = "";
    public string Name
    {
        get { return _Name; }
        set { _Name = value; Function_Update_by_CheckNames(value); }
    }
    public string Name_Formatted { get { int len = _Name.Length; if (len > 50) { len = 50; }  return _Name.Substring(0, len); } }

    private string _Min_Salary = "";
    private string _Max_Salary = "";

    public string GetMin { get { return _Min_Salary; } }
    public string GetMax { get { return _Max_Salary; } }

    public string _Salary 
        {
            get { return _Min_Salary;  }
            set {   string x = value.Replace(" ", ""); 
                    if (x.IndexOf("–") > -1) 
                        { 
                            _Max_Salary = x.Substring(x.IndexOf("–") + 1);
                            _Min_Salary = x.Substring(0, x.IndexOf("–") );
                        }
                    else
                        {
                            _Max_Salary = x;
                            _Min_Salary = _Max_Salary;
                        }
                } 
        }

    public string Address 
        {   get { return _Address;  }
            set { _Address = value; } }

    public string Function { get; set; }

    public string LinkHref { get; set; }

    public Proffi(string Name)
    {
       _Name=Name;
    }

    public void Function_Update_by_CheckNames(string Name)
    {
        _Name = Name;
        string tmp = Name.ToLower();

        bool is_FullStack = tmp.IndexOf("full stack") > 0;

        bool is_Junior = tmp.IndexOf("junior") >= 0 || tmp.IndexOf("джун") >= 0 || tmp.IndexOf("джуниор") >= 0 ||  tmp.IndexOf("интерн")>=0 || tmp.IndexOf("intern")>=0 || tmp.IndexOf("стажер")>=0;
        bool is_Middle = tmp.IndexOf("middle") >= 0 || tmp.IndexOf("мидл") >= 0 || tmp.IndexOf("миддл") >= 0;
        bool is_Senior = tmp.IndexOf("senior") >= 0 || tmp.IndexOf("синьор") >= 0 || tmp.IndexOf("синьёр") >= 0 ;
        bool is_Lead = tmp.IndexOf("lead") >= 0 || tmp.IndexOf("лид") >= 0 || tmp.IndexOf("тимлид") > 0 || tmp.IndexOf("ведущий программист") > 0; ;
        bool is_QA = tmp.IndexOf("qa") >=0 || tmp.IndexOf("тестир") >= 0  ;
        bool is_DevOps = tmp.IndexOf("devops") >= 0 || tmp.IndexOf("dev-ops") >= 0;

        Function = is_Junior ? "Джун" : (is_Middle ? "Средний" : (is_Senior ? "Синьор" : (is_Lead ? "Тимлид" : is_FullStack ? "Мастер" :  (is_DevOps ? "Devops" : (is_QA ? "Тестировщик" : "Специалист")))));

    }
    
}

class Site    :IDisposable
{
    /// <summary>
    /// Ссылки для обхода сайта
    /// </summary>
    private List<IElement> _linx = null;

    /// <summary>
    /// Свойство возварта ссылок
    /// </summary>
    public List<IElement> GetAllLinks { get { return _linx; }   }


    private List<IElement> _classes =null;
    /// <summary>
    /// Все элементы по классу из конструктора
    /// </summary>
    public List<IElement> GetAllClasses { get { return _classes; } }

    public Site(string url , string? get_data_by_class, int TimeOut=300) 
    {

        HttpClient http = new HttpClient();
        http.BaseAddress = new Uri(url);
        string data = "";

        bool tries = false;
        int retries = 0;
        do {
            try
            {
                data = http.GetStringAsync(new Uri(url)).Result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                tries = true;
                retries++;
                Thread.Sleep(TimeOut);
            }


            if (retries > 10)
            { 
                tries = true;
                Thread.Sleep(TimeOut);
            }
            
            if (retries > 20)
            {
                return;
            }

        } while (tries) ;

        //Защита от слепков
        data = data.Replace("/><", "/> <");
        data = data.Replace("><", "> <");
        data = data.Replace("₽", "Р");


        AngleSharp.Html.Parser.HtmlParser parser = new AngleSharp.Html.Parser.HtmlParser();
        var doc = parser.ParseDocument(data);

       _linx = doc.Links.Where(i => i != null).ToList();

        //# a11y-main-content > div:nth-child(74) > div > div.vacancy-serp-item-body > div

        if (get_data_by_class != null)
        { _classes = doc.GetElementsByClassName(get_data_by_class).Where(i => i != null).ToList<IElement>(); }
        

    }

    public void Dispose()
    {
        try {
            if( _linx != null ) _linx.Clear();
            if (_classes != null)  _classes.Clear();
        }
        finally 
        { 

            GC.Collect();
        }

    }

}
