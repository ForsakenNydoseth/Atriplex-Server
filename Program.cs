using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AtriplexServer
{
    internal static class Program
    {
        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int cmdShow);
        
        public static void Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            //Assign your own local IP here.
            listener.Prefixes.Add("http://192.168.1.200:80/");
            listener.Start();
            IntPtr hWnd = GetConsoleWindow();
            if (hWnd != IntPtr.Zero)
            {
                ShowWindow(hWnd, 0);
            }
            while (true)
            {
                try
                {
                    Resolve(listener.GetContext());
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        public static void Resolve(HttpListenerContext listenerContext)
        {
            Task.Run(() =>
            {
                Console.WriteLine("Received request.");
                if (listenerContext.Request.HttpMethod == "POST")
                {
                    var buffer = new byte[listenerContext.Request.ContentLength64];
                    listenerContext.Request.InputStream.Read(buffer, 0, buffer.Length);
                    var filename = listenerContext.Request.RawUrl.Remove(0, listenerContext.Request.RawUrl.LastIndexOf('/')).Trim('/').Replace("%", " ");
                    Directory.CreateDirectory("Scraped");
                    var selector = filename.Remove(0, filename.IndexOf('.'));
                    switch (selector)
                    {
                        case ".docx" or ".pptx" or ".ppt":
                        {
                            filename.WriteSR(ref buffer);  
                            break;
                        }
                        case ".pdf":
                        {
                            filename.WritePDF(ref buffer);  
                            break;
                        }
                        case ".zip" or ".rar":
                        {
                            filename.WriteZip(ref buffer);  
                            break;
                        }
                        case ".png" or ".jpeg" or ".jpg" or ".jfif":
                        {
                            filename.WriteImage(ref buffer);  
                            break;
                        }
                        default:
                        {
                            filename.WriteOther(ref buffer);  
                            break;
                        }
                    }
                    Console.WriteLine($"Wrote file: {listenerContext.Request.RawUrl}.");
                    listenerContext.Response.StatusCode = 200;
                    listenerContext.Response.Close();
                }
                else
                {
                    listenerContext.Response.StatusCode = 404;
                    listenerContext.Response.Close();
                } 
            });
        }

        public static void WriteOther(this string filename, ref byte[] buffer)
        {
            Directory.CreateDirectory("Scraped/Other");
            File.WriteAllBytes("Scraped/Other/" + filename, buffer);
        }
        
        public static void WriteImage(this string filename, ref byte[] buffer)
        {
            Directory.CreateDirectory("Scraped/Images");
            File.WriteAllBytes("Scraped/Images/" + filename, buffer);
        }

        public static void WriteZip(this string filename, ref byte[] buffer)
        {
            Directory.CreateDirectory("Scraped/Compressed");
            File.WriteAllBytes("Scraped/Compressed/" + filename, buffer);
        }

        public static void WritePDF(this string filename, ref byte[] buffer)
        {
            Directory.CreateDirectory("Scraped/PDF");
            File.WriteAllBytes("Scraped/PDF/" + filename, buffer);
        }
        
        public static void WriteSR(this string filename, ref byte[] buffer)
        {
            Directory.CreateDirectory("Scraped/WordOrPowerpoint");
            File.WriteAllBytes("Scraped/WordOrPowerpoint/" + filename, buffer);
        }
    }
}
