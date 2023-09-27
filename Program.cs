// See https://aka.ms/new-console-template for more information
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Io;
using ScanHH;
using System;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading;
using static System.Net.WebRequestMethods;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("Закачка данных...");

// Java
// https://hh.ru/search/vacancy?ored_clusters=true&search_field=name&search_field=company_name&search_field=description&text=java&enable_snippets=false&L_save_area=true&page=*PAGE*

//C#
// "https://hh.ru/search/vacancy?search_field=name&search_field=company_name&search_field=description&enable_snippets=false&L_save_area=true&employment=full&schedule=remote&text=%D1%80%D0%B0%D0%B7%D1%80%D0%B0%D0%B1%D0%BE%D1%82%D1%87%D0%B8%D0%BA+C%23&page=*PAGE*";

//Безопасник
//https://hh.ru/search/vacancy?ored_clusters=true&schedule=remote&search_field=company_name&search_field=name&search_field=description&text=%D1%81%D0%BF%D0%B5%D1%86%D0%B8%D0%B0%D0%BB%D0%B8%D1%81%D1%82+%D0%BF%D0%BE+%D0%B8%D0%BD%D1%84%D0%BE%D1%80%D0%BC%D0%B0%D1%86%D0%B8%D0%BE%D0%BD%D0%BD%D0%BE%D0%B9+%D0%B1%D0%B5%D0%B7%D0%BE%D0%BF%D0%B0%D1%81%D0%BD%D0%BE%D1%81%D1%82%D0%B8&enable_snippets=false&L_save_area=true&page=*PAGE*

//Кадры
//https://hh.ru/search/vacancy?ored_clusters=true&schedule=remote&area=113&search_field=name&search_field=company_name&search_field=description&text=%D0%BA%D0%B0%D0%B4%D1%80%D0%BE%D0%B2%D0%B8%D0%BA&enable_snippets=false&L_save_area=true

string test_url = "https://hh.ru/search/vacancy?ored_clusters=true&schedule=remote&area=113&search_field=name&search_field=company_name&search_field=description&text=%D0%BA%D0%B0%D0%B4%D1%80%D0%BE%D0%B2%D0%B8%D0%BA&enable_snippets=false&L_save_area=true&page=*PAGE*";


//Принимамем аргументы - имя файла конфига
var MyArgs = args.ToList();
Config cfg = new Config();
cfg.Target = test_url;

string sample_cfg = JsonSerializer.Serialize(cfg);
System.IO.File.WriteAllText("sample_cfg.json", sample_cfg);  


if (MyArgs.Count > 0)
{
    //конфиг
    var config = System.IO.File.ReadAllText(MyArgs[0]);
    cfg = JsonSerializer.Deserialize<Config>(config);

}

ParseHH parser = new ParseHH(cfg.Target,1, cfg.MaxList, cfg.Pause);

System.IO.File.WriteAllText(cfg.OutFile, "Name;Salary min;Salary max;Link;Grade;Skills;Address;Language\r\n");

Thread.Sleep(1000);
lock (parser)
{
    foreach (Proffi proffi in parser.proffi_list)
    {
        Console.WriteLine($" {proffi.Name_Formatted,-50} │ {proffi.GetMin,7} - {proffi.GetMax,7} │  {proffi.Grade,13}│ {proffi.LinkHref} │ {proffi.Skills,-30}");
        System.IO.File.AppendAllText(cfg.OutFile, $"{proffi.Name};{proffi.GetMin};{proffi.GetMax};{proffi.LinkHref};{proffi.Grade};{proffi.Skills};{proffi.Address};{proffi.Language} \r\n");

    }
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


        for (int ix = min_page; ix < max_page; ix++)
        {
            Task T = new Task(async () =>
            {
                int i = ix;

                 
                 using (Site site = new Site(url.Replace("*PAGE*", i.ToString()), "vacancy-serp-item-body",null,TimeOut))
                 {
                    if (site == null)
                    {
                        
                        return;
                    }

                    while (Update_AllLinks)
                            { Thread.Sleep(20); }
                    
                    Update_AllLinks=true;
                    if (site != null)
                        try { AllLinks.AddRange(site.GetAllLinks.AsParallel()); } catch {  return; }
                    Update_AllLinks = false;



                    /* foreach (var link in site.GetAllLinks)
                     {
                         Console.WriteLine($" {link.TextContent}-> {link.Attributes["href"].Value}");
                     }    */

                    //ФИльтр

                    Loading++;
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

                        if (proffi.LinkHref != "")
                        {
                            //дергаем списки        навыков
    
                            Site sub =new Site(proffi.LinkHref, "bloko-tag-list",null,TimeOut);
                            proffi.Skills = "";
                            //защита от косяка
                            try
                            {
                                foreach (Element e in sub.GetAllClasses)
                                {
                                    proffi.Skills += e.TextContent + " // ";
                                }
                            }
                            catch { }
                        }

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

        //сортировака по типам должности
        proffi_list.Sort(CompareProffies);

        Console.WriteLine(" готово!");



    } //ParseHH

    private static int CompareProffies(Proffi a, Proffi b)
    {
        return (a.Grade.CompareTo(b.Grade));
    }

}



public class Proffi
{

    /// <summary>
    /// Текущий курс евро и доллара
    /// </summary>
    public int EURO = 103;
    public int DOLLAR = 99;
    public double TENGE = 0.2;
    public double BEL_RUB = 38;


    private string _Name = "";
    private string _Address = "";
    private string _Skills = "";

    public string Name
    {
        get { return _Name; }
        set { _Name = value.Replace(";"," ").Replace("\r\n"," "); Function_Update_by_CheckNames(value); }
    }
    public string Name_Formatted { get { int len = _Name.Length; if (len > 50) { len = 50; }  return _Name.Substring(0, len); } }

    private string _Min_Salary = "";
    private string _Max_Salary = "";

    /// <summary>
    /// Размер вознаграждения без единииц
    /// </summary>
    public string GetMin { get { return _Min_Salary.Replace("р",""); } }
    public string GetMax { get { return _Max_Salary.Replace("р", "");  } }

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




            // - -- пЕРЕВОД ВАЛЮТ

                        if (_Min_Salary.IndexOf("br") > -1)
                        {
                            _Min_Salary = _Min_Salary.Replace("br", "");
                            _Min_Salary = ((int)(int.Parse(_Min_Salary) * BEL_RUB)).ToString() + "р";
                        }

                        if (_Max_Salary.IndexOf("br") > -1)
                        {
                            _Max_Salary = _Max_Salary.Replace("br", "");
                            _Max_Salary = ((int)(int.Parse(_Max_Salary) * BEL_RUB)).ToString() + "р";
                        }

                        if (_Min_Salary.IndexOf("₸") > -1)
                        {
                            _Min_Salary = _Min_Salary.Replace("₸", "");
                            _Min_Salary = ((int)(int.Parse(_Min_Salary) * TENGE)).ToString() + "р";
                        }

                        if (_Max_Salary.IndexOf("₸") > -1)
                        {
                            _Max_Salary = _Max_Salary.Replace("₸", "");
                            _Max_Salary = ((int)(int.Parse(_Max_Salary) * TENGE)).ToString() + "р";

                            if ((_Min_Salary.IndexOf("₸") == -1) && (_Min_Salary.IndexOf("р")==-1))
                            {
                                _Min_Salary = ((int)(int.Parse(_Min_Salary) * TENGE)).ToString() + "р";

                            }
                        }

                        if (_Min_Salary.IndexOf("€") > -1)
                        {
                            _Min_Salary = _Min_Salary.Replace("€", "");
                            _Min_Salary = ((int)  (int.Parse(_Min_Salary) * EURO)).ToString() + "р";
                        }


                        if (_Min_Salary.IndexOf("$") > -1)
                        {
                            _Min_Salary = _Min_Salary.Replace("$", "");
                            _Min_Salary = ((int)(int.Parse(_Min_Salary) * DOLLAR)).ToString() + "р";
                        }


                        if (_Max_Salary.IndexOf("€") > -1)
                        {
                            _Max_Salary = _Max_Salary.Replace("€", "");
                            _Max_Salary = ((int)(int.Parse(_Max_Salary) * EURO)).ToString() + "р";

                            if ((_Min_Salary.IndexOf("€") == -1)   && (_Min_Salary.IndexOf("р") == -1))
                            {
                                _Min_Salary = ((int)(int.Parse(_Min_Salary) * EURO)).ToString() + "р";
                
                            }

                        }


                        if (_Max_Salary.IndexOf("$") > -1)
                        {
                            _Max_Salary = _Max_Salary.Replace("$", "");
                            _Max_Salary = ((int)(int.Parse(_Max_Salary) * DOLLAR)).ToString() + "р";
                            if ((_Min_Salary.IndexOf("$") == -1)         && (_Min_Salary.IndexOf("р") == -1))
                            {
                                _Min_Salary = ((int)(int.Parse(_Min_Salary) * DOLLAR)).ToString() + "р";

                            }
                        }

            } 
        }

    public string Address 
        {   get { return _Address;  }
            set { _Address = value.Replace(";", " ").Replace("\r\n", " ");  } }

    public string Skills 
        { 
            get { return _Skills; } 
            set { _Skills = value.Replace(";", " ").Replace("\r\n", " "); } 
        }

    public string Grade { get; set; }
    public string Language { get; set; } = "";


    public string LinkHref { get; set; }

    public Proffi(string name)
    {
       Name=name;
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
        bool is_Admin = tmp.IndexOf("системный администратор") >= 0 || (tmp.IndexOf("системн") >= 0 && tmp.IndexOf("администрат") >= 0);
        bool is_Buh = tmp.IndexOf("бухгалтер") >= 0 ;
        bool is_HR = tmp.IndexOf("кадров") >= 0 || tmp.IndexOf("рекрутер") >= 0 || tmp.IndexOf("hr") >= 0;
        bool is_Leader = tmp.IndexOf("начальник") >= 0 || tmp.IndexOf("руководитель") >= 0 || tmp.IndexOf("директор") >= 0 || tmp.IndexOf("ceo") >= 0;
        bool is_Seller = tmp.IndexOf("отдела продаж") >= 0 || tmp.IndexOf("продавец") >= 0;
        bool is_Oper = tmp.IndexOf("оператор") >= 0 || tmp.IndexOf("call-") >= 0;
        bool is_Market = tmp.IndexOf("маркетолог") >= 0 || tmp.IndexOf("маркетинг") >= 0;

        Grade = is_Junior ? "Джун" : (is_Middle ? "Средний" : (is_Senior ? "Синьор" : (is_Lead ? "Тимлид" : is_FullStack ? "Мастер" :  (is_DevOps ? "Devops" : (is_QA ? "Тестировщик" : "Специалист")))));
        Grade = is_Buh ? "бухгалтер" : Grade;
        Grade = is_Admin ? "админстратор" : Grade;
        Grade = is_Leader ? "руководитель" : Grade;
        Grade = is_Seller ? "продавец" : Grade;
        Grade = is_Oper ? "оператор" : Grade;
        Grade = is_HR ? "HR" : Grade;
        Grade = is_Market ? "Маркетолог" : Grade;

        bool is_Java = tmp.IndexOf("java ") >= 0 || tmp.IndexOf("kotlin") >= 0;
        Grade = is_Java ?  Grade +" / java" : Grade;
        Language = is_Java ? "Java" : "";

        bool is_Javascript = tmp.IndexOf("javascript") >= 0 || tmp.IndexOf("typescript") >= 0 || tmp.IndexOf("vue") >= 0 || tmp.IndexOf(".js") >= 0 || tmp.IndexOf("react") >= 0 || tmp.IndexOf("angular") >= 0;
        Grade = is_Javascript ? Grade + " / javascript" : Grade;
        Language = is_Javascript ? "Javascript" : Language;

        bool is_Cpp = tmp.IndexOf("cpp") >= 0 || tmp.IndexOf("c++") >= 0;
        Grade = is_Cpp ? Grade + " / C++" : Grade;
        Language = is_Cpp ? "C++" : Language;


        bool is_CS = tmp.IndexOf("c#") >= 0 || tmp.IndexOf(".net") >= 0;
        Grade = is_CS ? Grade + " / C#" : Grade;
        Language = is_CS ? "C#" : Language;

        bool is_SQL = tmp.IndexOf("sql") >= 0 ||  tmp.IndexOf("pl/") >= 0;
        Grade = is_SQL ? Grade + " / SQL" : Grade;
        Language = is_SQL ? "SQL" : Language;

        bool is_pascal = tmp.IndexOf("delphi") >= 0 || tmp.IndexOf("pascal") >= 0;
        Grade = is_pascal ? Grade + " / Pascal" : Grade;
        Language = is_pascal ? "Pascal" : Language;

        bool is_PHP = tmp.IndexOf("php") >= 0 ;
        Grade = is_PHP ? Grade + " / PHP" : Grade;
        Language = is_PHP ? "PHP" : Language;

        bool is_Python = tmp.IndexOf("python") >= 0;
        Grade = is_Python ? Grade + " / Python" : Grade;
        Language = is_Python ? "Python" : Language;

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


    private List<IElement> _tags = null;
    public List<IElement> GetAllTags { get { return _tags; } }

    public Site(string url , string? get_data_by_class, string? get_data_by_tags, int TimeOut=300) 
    {

        HttpClient http = new HttpClient();
        http.BaseAddress = new Uri(url);
        string data = "";

        bool tries = false;
        int retries = 0;
        do {
            try
            {
                data =  http.GetStringAsync(new Uri(url)).Result;
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
        data = data.Replace("₽", "р");
        data = data.Replace("\u000A", " ");
        data = data.Replace("\r", " ");
        data = data.Replace("\n", " ");
        data = data.Replace("\t", " ");


        AngleSharp.Html.Parser.HtmlParser parser = new AngleSharp.Html.Parser.HtmlParser();
        var doc = parser.ParseDocument(data);

       _linx = doc.Links.Where(i => i != null).ToList();

        //# a11y-main-content > div:nth-child(74) > div > div.vacancy-serp-item-body > div

        if (get_data_by_class != null)
            { _classes = doc.GetElementsByClassName(get_data_by_class).Where(i => i != null).ToList<IElement>(); }

        if (get_data_by_tags != null)
            {
                _tags = doc.GetElementsByTagName(get_data_by_tags).ToList();
            }
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
