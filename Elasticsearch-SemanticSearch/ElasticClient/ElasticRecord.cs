using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch.ElasticClient
{
    public class ElasticRecord
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }
        public string NormalizedText { get; set; }
        public float[] Vector { get; set; }
        public double? Score { get; set; }
    }

}
