﻿using System.Collections.Generic;

namespace ErsatzTV.Core.Domain
{
    public class MultiCollection
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Collection> Collections { get; set; }
        public List<MultiCollectionItem> MultiCollectionItems { get; set; }
    }
}
