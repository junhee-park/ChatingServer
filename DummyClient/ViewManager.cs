using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyClient
{
    public interface IViewManager
    {
        void ShowText(string text);
    }

    public class ConsoleViewManager : IViewManager
    {
        public void ShowText(string text)
        {
            Console.WriteLine(text);
        }
    }

    
}
