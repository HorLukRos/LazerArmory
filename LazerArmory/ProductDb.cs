using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LazerArmory
{
    [ProtoContract]
    public class Client
    {
        [ProtoMember(1)]
        public string location { get; set; }

        [ProtoMember(13)]
        public string name { get; set; }
    }

    [ProtoContract]
    public class Product
    {
        [ProtoMember(1)]
        public string name { get; set; }

        [ProtoMember(2)]
        public string alias { get; set; }

        [ProtoMember(3)]
        public Client client { get; set; }

        [ProtoMember(6)]
        public string family { get; set; }
    }

    [ProtoContract]
    public class ProductDb
    {
        [ProtoMember(1)]
        public Product[] products { get; set; }

        [ProtoMember(7)]
        public string[] productNames { get; set; }
    }
}
