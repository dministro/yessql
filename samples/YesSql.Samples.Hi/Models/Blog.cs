using System.Collections.Generic;
using YesSql.Attributes;

namespace YesSql.Samples.Hi.Models
{
    [RelationContainer]
    public class Blog
    {
        public long Id { get; set; }
        public string Title { get; set; }
        [ReferencedProperty]
        public IEnumerable<BlogPost> Posts { get; set; }
        [ReferencedProperty]
        public BlogPost Highlighted { get; set; }
        [ReferencedProperty]
        public BlogPost Interested { get; set; }
    }
}
