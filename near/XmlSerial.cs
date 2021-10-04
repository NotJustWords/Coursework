using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace XmlSerial
{
    /// <summary>
    /// Шаблонный класс, сериализующий хмл
    /// </summary>
    class XmlSerialize
    {
        public static void save<T>( string fname, T obj )
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            TextWriter writer = new StreamWriter(fname);

            xml.Serialize(writer, obj);

            writer.Close();
        }

        public delegate void FillNew<T>(T obj);

        // Если файла нет, то создается пустая структура
        public static T load<T>( string fname ) where T : new()
        {
            if (!File.Exists(fname))
                return new T();

            XmlSerializer xml = new XmlSerializer(typeof(T));
            TextReader reader = new StreamReader(fname);

            T data = (T)xml.Deserialize(reader);

            reader.Close();

            return data;
        }
    }
}
