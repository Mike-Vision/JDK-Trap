using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json.Serialization;

namespace JDKTrap.UI.ViewModels.About
{
    public class ContributorItem
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string MarkdownText => $"[{Name}]({Url})";
    }

    public class GithubContributor
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }

    public class AboutViewModel : NotifyPropertyChangedViewModel
    {
        public string Version => string.Format(Strings.Menu_About_Version, App.Version);

        public BuildMetadataAttribute BuildMetadata => App.BuildMetadata;
            
        public string BuildTimestamp => BuildMetadata.Timestamp.ToFriendlyString();
        public string BuildCommitHashUrl => $"https://github.com/{App.ProjectRepository.Trim('/')}/commit/{BuildMetadata.CommitHash}";

        public Visibility BuildInformationVisibility => App.IsProductionBuild ? Visibility.Collapsed : Visibility.Visible;
        public Visibility BuildCommitVisibility => App.IsActionBuild ? Visibility.Visible : Visibility.Collapsed;

        public ObservableCollection<ContributorItem> Coders { get; } = new ObservableCollection<ContributorItem>();
        public ObservableCollection<ContributorItem> Contributors { get; } = new ObservableCollection<ContributorItem>();

        public AboutViewModel()
        {
            // Coders is just the owner: Mike-Vision
            Coders.Add(new ContributorItem 
            { 
                Name = "Mike-Vision", 
                Url = "https://github.com/Mike-Vision" 
            });

            _ = LoadContributorsAsync();
        }

        private async Task LoadContributorsAsync()
        {
            try
            {
                // Fetch contributors from GitHub API
                string url = $"https://api.github.com/repos/{App.ProjectRepository.Trim('/')}/contributors";
                var list = await JDKTrap.Utility.Http.GetJson<List<GithubContributor>>(url);
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        // Exclude Mike-Vision from contribution list as they are already in Coders
                        if (string.Equals(item.Login, "Mike-Vision", StringComparison.OrdinalIgnoreCase))
                            continue;

                        App.Current.Dispatcher.Invoke(() =>
                        {
                            Contributors.Add(new ContributorItem
                            {
                                Name = item.Login,
                                Url = item.HtmlUrl
                            });
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine("AboutViewModel", "Failed to load contributors from GitHub.");
                App.Logger.WriteException("AboutViewModel", ex);
            }
        }
    }
}
