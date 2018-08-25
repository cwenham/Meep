using System;
using System.Collections.Concurrent;
using System.Linq;

namespace MeepLib.Filters
{
    /// <summary>
    /// Record categorisation of messages
    /// </summary>
    /// <remarks>Categorisation could come from any module and method: range,
    /// pattern match, Bayes, neural net, etc.
    /// 
    /// <para>The module that made the classification is not recorded, only the
    /// name of the class.</para>
    /// 
    /// <para>Messages can be in multiple categories, and there can be as many
    /// categories as you want.</para></remarks>
    public static class Categories
    {
        public static void AddToCategory(string category, Guid messageID)
        {
            ConcurrentBag<Guid> catBag = null;
            if (!_categories.TryGetValue(category, out catBag))
            {
                catBag = new ConcurrentBag<Guid>();
                _categories.TryAdd(category, catBag);
            }

            catBag.Add(messageID);
        }

        public static bool InCategory(this Guid messageID, string category)
        {
            ConcurrentBag<Guid> catBag = null;
            if (!_categories.TryGetValue(category, out catBag))
                return false;

            return catBag.Contains(messageID);
        }

        // ToDo: serialise this to AHostProxy.Cache, which will take care of
        // persisting it.
        private static ConcurrentDictionary<string, ConcurrentBag<Guid>> _categories = new ConcurrentDictionary<string, ConcurrentBag<Guid>>();
    }
}
