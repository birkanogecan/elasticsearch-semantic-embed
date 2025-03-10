using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elasticsearch_SemanticSearch.Data
{
    public class ProductTY
    {
        [Key]
        public int Id { get; set; }
        public int PageIndex { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string BrandName { get; set; }
        public string CategoryName { get; set; }
    }
}
