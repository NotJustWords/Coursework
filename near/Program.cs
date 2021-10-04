using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using XmlSerial;

namespace near
{
    public class Program
    {
        static void Main(string[] args)
        {
            (new Program()).run();
        }

        // Списки файлов в панелях
        ItemList left = new ItemList(), right = new ItemList();

        // Текущая панель (0 или 1)
        int activePanel = 0;

        // Стиль отображения (по умолчанию)
        Style defStyle;

        // Начальная инициализация
        void init()
        {
            left.pos = new Rect(2, 1, 38, 22);
            left.panelh = left.pos.h;

            right.pos = new Rect(43, 1, left.pos.w, left.pos.h);
            right.panelh = left.panelh;

            defStyle = new Style();
            defStyle.def = new StyleItem(ConsoleColor.White, ConsoleColor.Black);
            defStyle.sel = new StyleItem(ConsoleColor.White, ConsoleColor.DarkBlue);
        }

        Settings sett;
        const string settFName = "settings.xml";

        void readSettings()
        {
            sett = XmlSerialize.load<Settings>(settFName);

            left.path = sett.left;
            right.path = sett.right;
        }

        void writeSettings()
        {
            sett.left = left.path;
            sett.right = right.path;

            XmlSerialize.save<Settings>(settFName, sett);
        }

        void run()
        {
            init();
            readSettings();

            bool end = false;
            bool forceReread = true;

            // Основной цикл
            while (!end)
            {
                if (forceReread)
                {
                    readDir(left);
                    readDir(right);
                    forceReread = false;
                }

                // Рисуем панели
                showList(left);
                showList(right);

                // Рисуем подписи к панелям
                defStyle.def.set();

                Console.SetCursorPosition(left.pos.x, left.pos.y + left.pos.h);
                Console.Write(left[left.cursor].descr);

                Console.SetCursorPosition(right.pos.x, right.pos.y + right.pos.h);
                Console.Write(right[right.cursor].descr);

                Console.SetCursorPosition(left.pos.x, left.pos.y2 + 1);
                Console.Write("F5 Copy  F6 Move  F8 Delete  F10 exit");

                ItemList cpanel = (activePanel == 0 ? left : right);
                cpanel.showCursor();

                var key = Console.ReadKey();

                // В текущей панели обработать курсорные клавиши
                cpanel.handleKey(key.Key);

                var item = cpanel[cpanel.cursor];

                // Действие
                if (key.Key == ConsoleKey.Enter)
                {
                    if ((item.flags & Item.Directory) != 0)
                    {
                        followDir(cpanel, item);

                        // После смены папки запоминаем пути
                        writeSettings(); 
                    }
                    else
                        // Открыть файл стандартным образом
                        System.Diagnostics.Process.Start(cpanel.path + "\\" + item.name);
                }

                // Действия над файлами (и папками)
                if ((key.Key == ConsoleKey.F5) | (key.Key == ConsoleKey.F6))
                {
                    ItemList opanel = (activePanel == 1 ? left : right);

                    doOp(cpanel, item, opanel, (key.Key == ConsoleKey.F5 ? fileAction.Copy : fileAction.Move) );

                    forceReread = true;
                }

                // Удаление
                if (key.Key == ConsoleKey.F8)
                {
                    doOp(cpanel, item, null, fileAction.Del);

                    forceReread = true;
                }


                // Возможно поправить скролл
                cpanel.fixStart();

                // Смена панели
                if (key.Key == ConsoleKey.Tab)
                    activePanel = 1 - activePanel;

                // Выход
                if (key.Key == ConsoleKey.F10)
                {
                    if (inputBox( "Exit program?", new string[] { "Yes", "No" }, 1 ) == 0)
                        end = true;
                }
            }
        }

        // Пройти в текущем итеме по папкам (или вверх, или вниз)
        private void followDir(ItemList cpanel, Item item)
        {
            string p = cpanel.path, f = "";
            if ((cpanel.cursor == 0) & (item.name == ".."))
            {
                int i = p.LastIndexOf("\\");
                f = p.Substring(i + 1);
                p = p.Substring(0, i);
            }
            else
            {
                p = p + "\\" + item.name;
            }
            cpanel.path = p;

            readDir(cpanel);

            if (f != "")
                cpanel.cursor = cpanel.FindIndex(it => it.name == f);
            else
                cpanel.cursor = 0;
        }

        // Операции с файлами
        enum fileAction { Copy, Move, Del };

        void doOp(ItemList fromlst, Item from, ItemList to, fileAction act )
        {
            if ((from.flags & Item.Directory) != 0)
            {                
                if (act == fileAction.Move)
                    Directory.Move(fromlst.path + "\\" + from.name, to.path);
                else if (act == fileAction.Del)
                    Directory.Delete(fromlst.path + "\\" + from.name, true);
            }
            else
            {
                if (act == fileAction.Copy)
                    File.Copy(fromlst.path + "\\" + from.name, to.path + "\\" + from.name);
                else if (act == fileAction.Move)
                    File.Move(fromlst.path + "\\" + from.name, to.path + "\\" + from.name);
                else if (act == fileAction.Del)
                    File.Delete(fromlst.path + "\\" + from.name);
            }
        }

        Style inputBoxStyle = null;

        // Предложение выбрать один элемент списка
        int inputBox(string msg, string[] items, int def = 0)
        {
            if (inputBoxStyle == null)
            {
                inputBoxStyle = new Style();
                inputBoxStyle.def = new StyleItem(ConsoleColor.White, ConsoleColor.Blue);
                inputBoxStyle.sel = new StyleItem(ConsoleColor.White, ConsoleColor.DarkBlue);
            }

            ItemList lst = new ItemList();

            int l = msg.Length;
            foreach (var itm in items)
            {
                lst.Add(new Item(" " + itm + " "));
                if (l < itm.Length)
                    l = itm.Length;
            }
            l += 2;

            lst.setCursor(def);

            Rect r = new Rect( 40 - msg.Length / 2 - 3, 10, l + 5, 4 + items.Length );

            lst.pos = new Rect(r.x + 1, r.y + 3, r.w - 2, items.Length);
            
            while (true)
            {
                inputBoxStyle.def.set();
                clear(r);

                Console.SetCursorPosition( r.x + 1, r.y + 1 );
                Console.Write( msg );

                showList( lst, inputBoxStyle );
                lst.showCursor();

                var key = Console.ReadKey();

                lst.handleKey(key.Key);

                if (key.Key == ConsoleKey.Enter)
                    return lst.cursor;
                if (key.Key == ConsoleKey.Escape)
                    return -1;
            }
        }

        // Очистка прямоугольника (стиль не выбираем)
        void clear(Rect r)
        {
            int i;
            string s = "";

            for (i = 0; i < r.w; i++)
                s += " ";

            for (i = 0; i < r.h; i++)
            {
                Console.SetCursorPosition(r.x, r.y + i);
                Console.Write(s);
            }
        }

        string getFName(string s)
        {
            int i = s.LastIndexOf( "\\" );
            if (i >= 0)
                s = s.Substring(i + 1);

            return s;
        }

        // Построить список папки
        void readDir(ItemList lst)
        {
            lst.Clear();

            string path = lst.path;
            if (path == "")
                path = "\\";

            if (path.Length > 1)
                lst.Add(new Item("..", Item.Directory));

            DirectoryInfo di = new DirectoryInfo(path);

            foreach( var d in di.GetDirectories() )
            {
                lst.Add(new Item(getFName(d.Name), Item.Directory, " <Directory> " ));
            }

            foreach (var f in di.GetFiles())
            {
                lst.Add(new Item(getFName(f.Name), 0, 
                    "Size " + f.Length + ". Attributes: " + f.Attributes));
            }

            lst.moveCursor(0);
        }

        // Показ списка на экране
        void showList(ItemList lst, Style style = null )
        {
            if (style == null)
                style = defStyle;

            var r = lst.pos;
            style.def.set();
            clear(r);

            for (int i = 0; i < r.h; i++)
            {
                if (lst.start + i >= lst.Count)
                    break;

                Console.SetCursorPosition(r.x, r.y + i);

                // Текуший элемент подсвечиваем
                if (lst.start + i == lst.cursor)
                    style.sel.set();
                else
                    style.def.set();

                string s = lst[ lst.start + i ].name;

                if (s.Length > r.w - 1)
                    s = s.Substring(0, r.w - 1) + "}";

                Console.Write(s);
            }
        }

        // Один элемент списка
        class Item
        {
            public string name, descr = "";
            public int flags = 0;

            public const int Directory = 1;

            public Item(string name, int fl = 0, string descr = "" )
            {
                this.name = name;
                flags = fl;
                this.descr = descr;
            }
        }

        // Список элементов
        class ItemList : List<Item>
        {
            public int cursor = 0, start = 0, panelh;
            public string path = "";
            public Rect pos;

            // Обработка кнопок
            public bool handleKey(ConsoleKey key)
            {
                if (key == ConsoleKey.DownArrow)
                {
                    moveCursor(1);
                    return true;
                }

                if (key == ConsoleKey.UpArrow)
                {
                    moveCursor(-1);
                    return true;
                }
                if (key == ConsoleKey.PageDown)
                {
                    moveCursor(panelh);
                    return true;
                }
                if (key == ConsoleKey.PageUp)
                {
                    moveCursor(-panelh);
                    return true;
                }
                if (key == ConsoleKey.Home)
                {
                    setCursor(0);
                    return true;
                }
                if (key == ConsoleKey.End)
                { 
                    setCursor(Count - 1);
                    return true;
                }

                return false;
            }

            public void setCursor(int v)
            {
                cursor = v;
                moveCursor(0);
            }

            public void moveCursor(int delta)
            {
                cursor += delta;
                if (cursor < 0) cursor = 0;
                if (cursor >= Count) cursor = Count - 1;
            }

            // Показать курсор на экране
            public void showCursor()
            {
                Console.SetCursorPosition(pos.x, pos.y + cursor - start);
            }

            public void fixStart()
            {
                if (cursor < start)
                    start = cursor;
                if (cursor >= start + panelh - 1)
                    start = cursor - (panelh - 1);
            }
        }

        // Класс прямоугольника
        class Rect
        {
            public int x, y, w, h;

            public Rect( int x = 0, int y = 0, int w = 1, int h = 1 )
            {
                this.x = x;
                this.y = y;
                this.w = w;
                this.h = h;
            }

            public int y2
            {
                get { return y + h; }
            }
        }

        public class Settings
        {
            public string left { get; set; }
            public string right { get; set; }

            public Settings()
            {
                left = "";
                right = "";
            }
        }
    }
}
