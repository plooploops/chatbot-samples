using EchoBot1.BO;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public static class SearchService
    {
        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/search/search-import-data-portal
        /// </summary>
        private static string SearchServiceName = "";
        private static string SearchServiceAdminApiKey = "";
        private static string SearchServiceQueryApiKey = "";

        private static string INDEX_NAME_SEARCH = "";
        private static SearchServiceClient CreateSearchServiceClient()
        {
            string searchServiceName = SearchServiceName;

            string adminApiKey = SearchServiceAdminApiKey;

            SearchServiceClient serviceClient = new SearchServiceClient(searchServiceName, new SearchCredentials(adminApiKey));

            return serviceClient;
        }

        private static SearchIndexClient CreateSearchIndexClient()
        {
            string searchServiceName = SearchServiceName;

            string queryApiKey = SearchServiceQueryApiKey;

            SearchIndexClient indexClient = new SearchIndexClient(searchServiceName, INDEX_NAME_SEARCH, new SearchCredentials(queryApiKey));

            return indexClient;
        }

        public static FacetResults FindFacets(string fauceton, string search)
        {
            ISearchIndexClient indexClient = CreateSearchIndexClient();

            SearchParameters parameters = new SearchParameters()
            {
                QueryType = QueryType.Full,
                Facets = new List<string>() { fauceton },
                Top = 0
            };

            DocumentSearchResult<Performance> results = indexClient.Documents.Search<Performance>(search ?? "*", parameters);

            return results.Facets;
        }

        public static List<Performance> FindPerformances(string bandName, string genre, string day)
        {
            ISearchIndexClient indexClient = CreateSearchIndexClient();

            SearchParameters parameters = new SearchParameters()
            {
                Filter = GetFilterString(genre, day),
                QueryType = QueryType.Full
            };

            bandName = bandName + "*";

            DocumentSearchResult<Performance> results = indexClient.Documents.Search<Performance>(bandName ?? "*", parameters);

            return results.Results.Select(i => i.Document).ToList();
        }

        private static string GetFilterString(string genre, string day)
        {
            StringBuilder filter = new StringBuilder();

            if (!string.IsNullOrEmpty(genre))
            {
                filter.Append("genre eq '" + genre + "'");
            }

            if (!string.IsNullOrEmpty(day))
            {
                if (filter.Length > 0)
                {
                    filter.Append(" and ");
                }

                filter.Append("day eq '" + day + "'");
            }

            return filter.ToString();
        }
    }
}
