using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using YamlDotNet.Serialization;

namespace CLIDownloader;

internal record DownloadData
{
   [YamlMember(Alias = "url")]
   public string URL { get; set; } = null!;

   [YamlMember(Alias = "file")]
   public string File { get; set; } = null!;

   [YamlMember(Alias = "sha1")]
   public string? SHA1 { get; set; }

   [YamlMember(Alias = "sha256")]
   public string? SHA256 { get; set; }

   [YamlMember(Alias = "overwrite")]
   public bool? Overwrite { get; set; }
}