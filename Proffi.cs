using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScanHH
{

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
            set
            {
                _Name = value.Replace(";", " ").Replace("\r\n", " ");
                Function_Update_by_CheckNames(_Name);
            }
        }

        /// <summary>
        /// Форматированное название
        /// </summary>
        public string Name_Formatted { get { int len = _Name.Length; if (len > 50) { len = 50; } return _Name.Substring(0, len); } }


        private string _Min_Salary = "";
        private string _Max_Salary = "";

        /// <summary>
        /// Размер вознаграждения без единииц в руб
        /// </summary>
        public string GetMin { get { return _Min_Salary.Replace("р", "").Replace(" ", ""); } }
        public string GetMax { get { return _Max_Salary.Replace("р", "").Replace(" ", ""); } }

        public string _Salary
        {
            get { return _Min_Salary; }
            set
            {
                string x = value.Replace(" ", "");
                if (x.IndexOf("–") > -1)
                {
                    _Max_Salary = x.Substring(x.IndexOf("–") + 1);
                    _Min_Salary = x.Substring(0, x.IndexOf("–"));
                }
                else
                {
                    _Max_Salary = x;
                    _Min_Salary = _Max_Salary;
                }


                if (x.IndexOf("до") > 0)
                {
                    _Max_Salary = x.Substring(x.IndexOf("до") + 2).Trim();

                    _Min_Salary = x.Substring(0, x.IndexOf("до")).Replace("от", "").Trim();

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
                _Min_Salary = _Min_Salary.Replace(" ", "");
                _Max_Salary = _Max_Salary.Replace(" ", "");

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

                    if ((_Min_Salary.IndexOf("₸") == -1) && (_Min_Salary.IndexOf("р") == -1))
                    {
                        _Min_Salary = ((int)(int.Parse(_Min_Salary) * TENGE)).ToString() + "р";

                    }
                }

                if (_Min_Salary.IndexOf("€") > -1)
                {
                    _Min_Salary = _Min_Salary.Replace("€", "");
                    _Min_Salary = ((int)(int.Parse(_Min_Salary) * EURO)).ToString() + "р";
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

                    if ((_Min_Salary.IndexOf("€") == -1) && (_Min_Salary.IndexOf("р") == -1))
                    {
                        _Min_Salary = ((int)(int.Parse(_Min_Salary) * EURO)).ToString() + "р";

                    }

                }


                if (_Max_Salary.IndexOf("$") > -1)
                {
                    _Max_Salary = _Max_Salary.Replace("$", "");
                    _Max_Salary = ((int)(int.Parse(_Max_Salary) * DOLLAR)).ToString() + "р";
                    if ((_Min_Salary.IndexOf("$") == -1) && (_Min_Salary.IndexOf("р") == -1))
                    {
                        _Min_Salary = ((int)(int.Parse(_Min_Salary) * DOLLAR)).ToString() + "р";

                    }
                }

                _Max_Salary = _Max_Salary.Replace(" ", "");
                _Min_Salary = _Min_Salary.Replace(" ", "");
            }
        }

        public string Address
        {
            get { return _Address; }
            set { _Address = value.Replace(";", " ").Replace("\r\n", " "); }
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
            Name = name;
        }

        public void Function_Update_by_CheckNames(string Name)
        {
            _Name = Name;
            string tmp = Name.ToLower();

            bool is_FullStack = tmp.IndexOf("full stack") > 0;

            bool is_Junior = tmp.IndexOf("junior") >= 0 || tmp.IndexOf("джун") >= 0 || tmp.IndexOf("джуниор") >= 0 || tmp.IndexOf("интерн") >= 0 || tmp.IndexOf("intern") >= 0 || tmp.IndexOf("стажер") >= 0;
            bool is_Middle = tmp.IndexOf("middle") >= 0 || tmp.IndexOf("мидл") >= 0 || tmp.IndexOf("миддл") >= 0;
            bool is_Senior = tmp.IndexOf("senior") >= 0 || tmp.IndexOf("синьор") >= 0 || tmp.IndexOf("синьёр") >= 0;
            bool is_Lead = tmp.IndexOf("lead") >= 0 || tmp.IndexOf("лид") >= 0 || tmp.IndexOf("тимлид") > 0 || tmp.IndexOf("ведущий программист") > 0; ;
            bool is_QA = tmp.IndexOf("qa") >= 0 || tmp.IndexOf("тестир") >= 0;
            bool is_DevOps = tmp.IndexOf("devops") >= 0 || tmp.IndexOf("dev-ops") >= 0;
            bool is_Admin = tmp.IndexOf("системный администратор") >= 0 || (tmp.IndexOf("системн") >= 0 && tmp.IndexOf("администрат") >= 0);
            bool is_Buh = tmp.IndexOf("бухгалтер") >= 0;
            bool is_HR = tmp.IndexOf("кадров") >= 0 || tmp.IndexOf("рекрутер") >= 0 || tmp.IndexOf("hr") >= 0;
            bool is_Leader = tmp.IndexOf("начальник") >= 0 || tmp.IndexOf("руководитель") >= 0 || tmp.IndexOf("директор") >= 0 || tmp.IndexOf("ceo") >= 0;
            bool is_Seller = tmp.IndexOf("отдела продаж") >= 0 || tmp.IndexOf("продавец") >= 0;
            bool is_Oper = tmp.IndexOf("оператор") >= 0 || tmp.IndexOf("call-") >= 0;
            bool is_Market = tmp.IndexOf("маркетолог") >= 0 || tmp.IndexOf("маркетинг") >= 0;

            Grade = is_Junior ? "Джун" : (is_Middle ? "Средний" : (is_Senior ? "Синьор" : (is_Lead ? "Тимлид" : is_FullStack ? "Мастер" : (is_DevOps ? "Devops" : (is_QA ? "Тестировщик" : "Специалист")))));
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

            bool is_SQL = tmp.IndexOf("sql") >= 0 || tmp.IndexOf("pl/") >= 0;

            Language = is_SQL ? "SQL" : Language;

            bool is_pascal = tmp.IndexOf("delphi") >= 0 || tmp.IndexOf("pascal") >= 0;

            Language = is_pascal ? "Pascal" : Language;

            bool is_PHP = tmp.IndexOf("php") >= 0;

            Language = is_PHP ? "PHP" : Language;

            bool is_Python = tmp.IndexOf("python") >= 0;

            Language = is_Python ? "Python" : Language;

        }

    }
}
