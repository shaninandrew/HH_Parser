// See https://aka.ms/new-console-template for more information
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Io;
using ScanHH;
using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Linq;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

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

JsonSerializerOptions options = new JsonSerializerOptions();
options.WriteIndented = true;

string sample_cfg = JsonSerializer.Serialize(cfg, options   );
System.IO.File.WriteAllText("sample_cfg.json", sample_cfg);  


if (MyArgs.Count > 0)
{
    //конфиг
    var config = System.IO.File.ReadAllText(MyArgs[0]);
    cfg = JsonSerializer.Deserialize<Config>(config);

}

ParseHH parser = new ParseHH(cfg.Target,1, cfg.MaxList, cfg.Pause);

System.IO.File.WriteAllText(cfg.OutFile, "Name;Salary min;Salary max;Link;Grade;Skills;Address;Language;Response;Task\r\n");

Thread.Sleep(1000);


    foreach (Proffi proffi in parser.proffi_list_all)
    {
        Console.WriteLine($" {proffi.Name_Formatted,-50} │ {proffi.GetMin,7} - {proffi.GetMax,7} │  {proffi.Grade,13}│ {proffi.LinkHref} │ {proffi.Skills,-30}");
        System.IO.File.AppendAllText(cfg.OutFile, $"{proffi.Name};{proffi.GetMin};{proffi.GetMax};{proffi.LinkHref};{proffi.Grade};{proffi.Skills};{proffi.Address};{proffi.Language};{proffi.Response};\"{proffi.Task}\" \r\n");

    }


//https://career.habr.com/vacancies?page=2&type=suitable



    /// <summary>
    /// Класс для обработки сайта HH.ru
    /// </summary>
public class ParseHH
{
  
    public List<Proffi> proffi_list_all = null;
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
        proffi_list_all = new List<Proffi>();

        bool Update_AllLinks = false;

     
        Console.Write("Закачка: ");
        Task<Proffi[]> [] tasks = new  Task<Proffi[]>[max_page - min_page];

        //Запуск задач

        int Loading = 0;

        for (int ix = min_page; ix < max_page; ix++)
        {
            Loading++;
            Console.Write("-");
            Task<Proffi[]> T = ProcessPage(url, ix);

            T.ContinueWith(t => 
                {
                    
                    Console.Write("x"); 
                    Loading--;

                    if (T == null) return;
                    if (T.Result == null) return;


                    proffi_list_all.AddRange(T.Result);
                    proffi_list_all = proffi_list_all.Distinct().ToList();
                    GC.Collect();
                });

        }

        Console.WriteLine("");
        Console.Write("Доделываем:");

        do
        {
            Console.Write(".");
            Thread.Sleep(TimeOut*2);
            GC.Collect();
        } while (Loading > 1) ;

        AllLinks = AllLinks.Distinct().ToList();
        proffi_list_all = proffi_list_all.Distinct().ToList();

        //сортировака по типам должности
        proffi_list_all.Sort(CompareProffies);
        Console.WriteLine(" готово!");

    } //ParseHH

    private static int CompareProffies(Proffi a, Proffi b)
    {
        return (a.Grade.CompareTo(b.Grade));
    }


    async Task<Proffi[]> ProcessPage(string url, int page_index)
    {
        Uri check = new Uri(url);

        if (check.Host.ToLower() == "hh.ru")
           return   (   await ProcessPageHH(url, page_index));

        if (check.Host.ToLower() == "career.habr.com")
            return  (   await ProcessPageHabr(url, page_index));
        
        return null;
    }

    /// <summary>
    /// Обработка страницы 
    /// </summary>
    /// <param name="url"></param>
    /// <param name="page_index"></param>
    /// <returns></returns>
    async Task<Proffi[]> ProcessPageHH(string url ,int page_index )
    {
        int i = page_index;
        List<Proffi> proffi_list = null;

        using (Site site = new Site(url.Replace("*PAGE*", i.ToString()), "vacancy-serp-item-body", null, TimeOut))
        {
            if (site == null)
            {
                return (null);
            }

            //Лоакльный список
            proffi_list = new List<Proffi>();
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
                        }
                        catch { }
                    }
                    if (span.ClassName == "bloko-header-section-2")
                        proffi._Salary = span.TextContent.ToLower().Replace("  ", " ").Replace("?", "");

                }//foreac
                 //
                proffi.Address = item.GetElementsByClassName("bloko-text").First().TextContent;

                // g-user-content  - функции
                //proffi._Function = item.GetElementsByClassName("g-user-content").First().TextContent;

                if (proffi.LinkHref != "")
                {
                    //дергаем списки        навыков

                    Site sub = new Site(proffi.LinkHref, "bloko-tag-list", null, TimeOut);
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

                    //если был отклик
                    try
                    {
                        proffi.Response = sub.GetAllTags.Where(t => t.ClassName == "vacancy-response").First().TextContent.Replace(";", "").Replace("\r\n", "");
                    }
                    catch { }
                    try 
                    {
                        proffi.Task = sub.GetAllTags.Where(t => t.ClassName == "g-user-content").First().TextContent.Replace(";", "").Replace("\r\n", "");
                    }
                    catch { }


                }

                //выгружаем в промежзуточный кэш
                proffi_list.Add(proffi);


            }     //for
            //чистим дубли
            
        } //using
        
        //отдаем кэш с аналитикой
        return proffi_list.Distinct().ToArray(); 
    }



    async Task<Proffi[]> ProcessPageHabr(string url, int page_index)
    {
        int i = page_index;
        List<Proffi> proffi_list = null;

        //body > div.page - container > div > div > div.content - wrapper > div > div > div.section - group.section - group--gap - medium > div.transition - expand > div > div > div.section - group.section - group--gap - medium > div:nth - child(1) > div > div > div > 
        using (Site site = new Site(url.Replace("*PAGE*", i.ToString()), "vacancy-card__info", null, TimeOut))
        {
            if (site == null)
            {
                return (null);
            }

            //Лоакльный список
            proffi_list = new List<Proffi>();
            foreach (IElement item in site.GetAllClasses)
            {
                Proffi proffi = new Proffi("");
                //  body > div.page-container > div > div > div.content-wrapper > div > div > div.section-group.section-group--gap-medium > div.transition-expand > div > div > div.section-group.section-group--gap-medium > div:nth-child(7) > div > div > div > div.vacancy-card__info
                
                foreach (var span in item.Children)
                {
                  //  Console.WriteLine($"{span.TagName}  {span.ClassName} {span.TextContent}");

                    if (span.ClassName.Equals ("vacancy-card__info"))
                    {
                        proffi.Name         = span.GetElementsByClassName("vacancy-card__title").First().TextContent;
                        proffi._Salary      = span.GetElementsByClassName("vacancy-card__salary").First().TextContent;
                        proffi.LinkHref     = span.GetElementsByTagName("a").First().Attributes["href"].Value;
                        proffi.Address      = item.GetElementsByClassName("vacancy-card__company").First().TextContent;
                    }

                    if (span.ClassName == "vacancy-card__title")
                        proffi.Name = span.TextContent;

                    if (span.ClassName == "vacancy-card__salary")
                        proffi._Salary = span.TextContent.ToLower();

                    if (span.ClassName == "vacancy-card__company")
                        proffi.Address = span.TextContent;

                    if (span.ClassName == "vacancy-card__skills")
                    {
                        proffi.Grade = span.Children.First().TextContent;
                        proffi.Skills = span.TextContent;
                    }


                }//foreach

                //структура спанов
               /*
                * var childs = item.GetElementsByClassName("vacancy-card__skills").Children("inline-list");

                proffi.Grade = childs.First().TextContent;
                proffi.Skills = "";
                foreach (IElement e in childs)
                {
                    if (e != childs.First())
                    { proffi.Skills += e.TextContent; }
                }    
               */

                // g-user-content  - функции
                //proffi._Function = item.GetElementsByClassName("g-user-content").First().TextContent;

                /*
                if (proffi.LinkHref != "")
                {
                    //дергаем списки        навыков

                    Site sub = new Site(proffi.LinkHref, "basic-section--appearance-vacancy-description", null, TimeOut);
                    proffi.Skills = "";
                    //защита от косяка
                    try
                    {
                        foreach (Element e in sub.GetAllClasses)
                        {
                            proffi.Task += e.TextContent + " // ";
                        }
                    }
                    catch { }

                }
                */

                proffi.Task = proffi.Task.Trim();
                proffi.Address = proffi.Address.Trim();
                proffi.Grade = proffi.Grade.Trim();
                proffi.Name = proffi.Name.Trim();
                proffi.Skills = proffi.Skills.Trim();

                //выгружаем в промежзуточный кэш
                proffi_list.Add(proffi);


            }     //for
                  //чистим дубли

        } //using

        //отдаем кэш с аналитикой
        return proffi_list.Distinct().ToArray();
    }

}


/// <summary>
/// Структура данных Профи
/// </summary>

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

    /// <summary>
    /// отзыв - если зарегистрированный опрос то будет видно, обычно скрыто
    /// </summary>
    public string Response = "";

    /// <summary>
    /// Возлагаемые обязанности. Описание вакансии
    /// </summary>
    public string Task = "";
    
    /// <summary>
    /// Название вакансии
    /// </summary>
    public string Name
    {
        get { return _Name; }
        set { 
            _Name = value.Replace(";"," ").Replace("\r\n"," "); 
            Function_Update_by_CheckNames(_Name); }
    }

    /// <summary>
    /// Форматированное название
    /// </summary>
    public string Name_Formatted { get { int len = _Name.Length; if (len > 50) { len = 50; }  return _Name.Substring(0, len); } }


    private string _Min_Salary = "";
    private string _Max_Salary = "";

    /// <summary>
    /// Размер вознаграждения без единииц в руб
    /// </summary>
    public string GetMin { get { return _Min_Salary.Replace("р","").Replace(" ",""); } }
    public string GetMax { get { return _Max_Salary.Replace("р", "").Replace(" ", "");  } }

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


                    if (x.IndexOf("до")>0)
                    {
                        _Max_Salary =  x.Substring(x.IndexOf("до") + 2).Trim();
                        
                        _Min_Salary = x.Substring(0,x.IndexOf("до") ).Replace("от","").Trim();

                        _Max_Salary = _Max_Salary.Replace("р", "").Trim(); ;
                        _Min_Salary = _Min_Salary.Replace("р", "").Trim(); ;
                
                        _Max_Salary = _Max_Salary.Replace("от", "").Trim(); ;
                        _Min_Salary = _Min_Salary.Replace("от", "").Trim(); ;

                    }

                if (x.IndexOf("от") > 0)
                {


                    _Min_Salary = _Max_Salary.Replace("от", "").Trim(); ;

                    _Max_Salary = _Min_Salary;

                    _Max_Salary = _Max_Salary.Replace("р", "").Trim(); ;
                    _Min_Salary = _Min_Salary.Replace("р", "").Trim(); ;


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
    {   
        get { return _Address;  }
        set { _Address = value.Replace(";", " ").Replace("\r\n", " ");  } 
    }

    public string Skills 
    { 
        get { return _Skills; } 
        set { _Skills = value.Replace(";", " ").Replace("\r\n", " "); } 
    }

    /// <summary>
    /// класс должности
    /// </summary>
    public string Grade { get; set; }

    /// <summary>
    /// Язык программирования
    /// </summary>
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
        
        Language = is_Java ? "Java" : "";

        bool is_Javascript = tmp.IndexOf("javascript") >= 0 || tmp.IndexOf("typescript") >= 0 || tmp.IndexOf("vue") >= 0 || tmp.IndexOf(".js") >= 0 || tmp.IndexOf("react") >= 0 || tmp.IndexOf("angular") >= 0;
        
        Language = is_Javascript ? "Javascript" : Language;

        bool is_Cpp = tmp.IndexOf("cpp") >= 0 || tmp.IndexOf("c++") >= 0;
        
        Language = is_Cpp ? "C++" : Language;

        bool is_CS = tmp.IndexOf("c#") >= 0 || tmp.IndexOf(".net") >= 0;
        
        Language = is_CS ? "C#" : Language;

        bool is_SQL = tmp.IndexOf("sql") >= 0 ||  tmp.IndexOf("pl/") >= 0;
        
        Language = is_SQL ? "SQL" : Language;

        bool is_pascal = tmp.IndexOf("delphi") >= 0 || tmp.IndexOf("pascal") >= 0;
        
        Language = is_pascal ? "Pascal" : Language;

        bool is_PHP = tmp.IndexOf("php") >= 0 ;
        
        Language = is_PHP ? "PHP" : Language;

        bool is_Python = tmp.IndexOf("python") >= 0;
        
        Language = is_Python ? "Python" : Language;

    }

}

class Site   :IDisposable
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


    private async  Task<StringBuilder> Load(string url, int TimeOut)
    {
        HttpClient http = new HttpClient();
        http.BaseAddress = new Uri(url);
        bool tries = false;
        int retries = 0;

        StringBuilder data = new StringBuilder("");

        do
        {
            try
            {
                data.Append( await http.GetStringAsync(new Uri(url)));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                tries = true;
                retries++;
                Task.Delay(TimeOut);
            }


            if (retries > 10)
            {
                tries = true;
                Task.Delay(TimeOut*2);
            }

            if (retries > 20)
            {
                break;
            }

        } while (tries);

        http.Dispose();
        return data;

    }

    public Site(string url , string? get_data_by_class, string? get_data_by_tags, int TimeOut=300) 
    {


        StringBuilder data = null;

        Task<StringBuilder> t= Load(url, TimeOut);
        t.Wait(TimeOut);

        //не получилось ну фиг с ним
        if ( t.IsFaulted ) { return; }
        data = t.Result;
          
        //Защита от слепков
         data.Replace("/><", "/> <");
         data.Replace("><", "> <");
         data.Replace("₽", "р");
         data.Replace("\u000A", " ");
         data.Replace("\r", " ");
         data.Replace("\n", " ");
         data.Replace("\t", " ");
         data.Replace("•", "");


        AngleSharp.Html.Parser.HtmlParser parser = new AngleSharp.Html.Parser.HtmlParser();
        var doc = parser.ParseDocument(data.ToString());

       _linx = doc.Links.Where(i => i != null).ToList();

        //# a11y-main-content > div:nth-child(74) > div > div.vacancy-serp-item-body > div

        if (get_data_by_class != null)
            { _classes = doc.GetElementsByClassName(get_data_by_class).Where(i => i != null).ToList<IElement>(); }

        if (get_data_by_tags != null)
        {
            _tags = doc.GetElementsByTagName(get_data_by_tags).ToList();
        }
        else
        {
            _tags = doc.All.ToList();
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
