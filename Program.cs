// See https://aka.ms/new-console-template for more information
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html;
using AngleSharp.Io;



Console.WriteLine("Parse data");
string test_url = "https://hh.ru/vacancies/administrator?hhtmFromLabel=rainbow_profession&hhtmFrom=main";

using (Site site = new Site(test_url, "vacancy-serp-item-body"))
{
    foreach (var link in site.GetAllLinks)
    {
        Console.WriteLine($" {link.TextContent}");
    }

    //ФИльтр
    List<Proffi> proffi_list= new List<Proffi>();

    foreach (IElement item in site.GetAllClasses)
    {
       

        Proffi proffi = new Proffi("");

        foreach (var span in item.GetElementsByTagName("span"))
        {
            // serp-item__title - Name 
            // bloko-header-section-2  - ЗП
            if (span.ClassName == null)
                proffi._Name = span.TextContent;
            if (span.ClassName == "bloko-header-section-2")
                proffi._Salary = span.TextContent.ToLower().Replace(" ","").Replace("от","").Replace("до","").Replace("?","");
            
        }//foreac
         //
        proffi._Address = item.GetElementsByClassName("bloko-text").First().TextContent;

        // g-user-content  - функции
        //proffi._Function = item.GetElementsByClassName("g-user-content").First().TextContent;

        proffi_list.Add(proffi);

    }



    foreach (Proffi proffi in proffi_list)
    {
        Console.WriteLine($" {proffi._Name} // {proffi._Salary}  // {proffi._Address} // {proffi._Function} ");
    }
} //using


public class Proffi
{                                  
    public string _Name { get; set;  }
    public string _Salary { get; set; }
    public string _Address { get; set; }

    public string _Function { get; set; }

    public Proffi(string Name)
    {
            Name = Name;
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

    public Site(string url , string? get_data_by_class) 
    {

        HttpClient http = new HttpClient();
        http.BaseAddress = new Uri(url);
        string data = http.GetStringAsync(new Uri(url)).Result;
        //Защита от слепков
        data = data.Replace("/><", "/> <");
        data = data.Replace("><", "> <");
        data = data.Replace("₽", "");


        AngleSharp.Html.Parser.HtmlParser parser = new AngleSharp.Html.Parser.HtmlParser();
        var doc = parser.ParseDocument(data);

       _linx = doc.Links.Where(i => i != null).ToList();

        //# a11y-main-content > div:nth-child(74) > div > div.vacancy-serp-item-body > div

        if (get_data_by_class != null)
        { _classes = doc.GetElementsByClassName(get_data_by_class).Where(i => i != null).ToList<IElement>(); }
        

    }

    public  void Dispose()
    {
        _linx.Clear();
        _classes.Clear();

     GC.Collect();

    }

}
