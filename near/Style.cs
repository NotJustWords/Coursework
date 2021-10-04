using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace near
{
    // Описание одного стиля
    class StyleItem
    {
        public ConsoleColor fore, back;

        public StyleItem(ConsoleColor f, ConsoleColor b)
        {
            fore = f;
            back = b;
        }

        public void set()
        {
            Console.BackgroundColor = back;
            Console.ForegroundColor = fore;
        }
    }

    // Стиль отображения
    class Style
    {
        public StyleItem def, sel;

        // public List<StyleItem> styles = new List<StyleItem>();
    }
}
