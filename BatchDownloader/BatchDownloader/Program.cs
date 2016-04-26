﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BatchDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            
            var url = args[0];
            var user = args[1];
            var pass = args[2];
            var drive = args[3];
            

            
            var itemQ = new Queue<DirectoryItem>();
            itemQ.Enqueue(new DirectoryItem(null) { Name = "/TV Shows/" });

            while(itemQ.Count > 0)
            {
                var item = itemQ.Dequeue();
                var turl = url + item.ToFullPathString();
                Console.WriteLine(turl);
                var items = GetItems(CreateRequest(turl, user, pass));
                foreach (var i in items)
                {
                    if (i.Value)
                    {

                        itemQ.Enqueue(new DirectoryItem(item) { Name = i.Key });
                    }
                    else
                    {
                        var fileItem = new FileItem(item) { Name = i.Key };
                        var localPath = drive + fileItem.Parent.ToFullPathString();
                        if (!Directory.Exists(localPath))
                        {
                            Directory.CreateDirectory(localPath);
                        }
                        var str = File.OpenWrite(localPath);
                        DownloadFile(CreateRequest(url + fileItem.ToFullPathString(), user, pass), str);
                        str.Dispose();
                    }
                }
            } 

            Console.ReadLine();
        }
        private static void DownloadFile(FtpWebRequest req, FileStream output)
        {
            req.UseBinary = true;
            req.Method = WebRequestMethods.Ftp.DownloadFile;
            using (var resp = req.GetResponse())
            {
                byte[] buffer = new byte[1000000];
                while (true)
                {
                    var read = resp.GetResponseStream().Read(buffer, 0, buffer.Length);
                    output.Write(buffer, 0, read);
                    if (read <= 0)
                    {
                        output.Flush();
                        break;
                    }
                }
            }
        }

        private static IReadOnlyDictionary<string, bool> GetItems(FtpWebRequest request)
        {
            var l = new Dictionary<string, bool>();
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            using (var resp = request.GetResponse()) {
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    while (!reader.EndOfStream)
                    {
                        var parts = reader.ReadLine().Split(new char[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                        
                        l.Add(parts[3], parts[2].Contains("DIR"));
                    }
                }
            }
            return l;
        }

        

        private static FtpWebRequest CreateRequest(string url, string user, string pass)
        {
            var request = (FtpWebRequest)WebRequest.Create(url);
            request.Credentials = new NetworkCredential(user, pass);
            request.Proxy = null;
            request.EnableSsl = false;
            return request;
        }
    }

    public abstract class Item
    {
        protected Item(ItemType itemType) {
            ItemType = itemType;
        }
        public ItemType ItemType { get; private set; }
        public string Name { get; set; }

        public abstract string ToFullPathString();
    }

    public enum ItemType
    {
        File = 1,
        Directory = 2
    }

    public class DirectoryItem : Item
    {
        public DirectoryItem(DirectoryItem parent) : base(ItemType.Directory)
        {
            Parent = parent;
        }

        public DirectoryItem Parent { get; private set; }

        public override string ToFullPathString()
        {
            var path = new Stack<string>();
            path.Push(Name);

            var currParent = Parent;
            while(currParent != null)
            {
                path.Push(currParent.Name + "/");
                currParent = currParent.Parent;
            }

            var str = "";
            while(path.Count > 0)
            {
                str += path.Pop();
            }
            return str;
        }
    }

    public class FileItem : Item
    {
        public FileItem(DirectoryItem parent) : base(ItemType.Directory)
        {
            Parent = parent;
        }

        public DirectoryItem Parent { get; private set; }

        public override string ToFullPathString()
        {
            return Parent.ToFullPathString() + "/" + Name;
        }
    }
}
