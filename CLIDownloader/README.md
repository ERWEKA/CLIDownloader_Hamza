# CLIDownloader
Erweka's take home test

Erweka work trail
Project: CLI Download Manager
Create a small CLI tool to download multiple files from various sources in parallel. As input
you get a configuration file (.yaml) which contains all necessary data you need for the
download job. Beside downloading files, the tool should verify the existence of the
downloaded files.
● Commands:
○ download [--verbose] [--dry-run] [parallel-downloads=N] config.yml
○ validate [--verbose] config.yml
● Requirements:
○ C# / .NET Core
○ runs on Linux / Windows
● Expectations:
○ usage of DI (requirement)
○ parallel downloads (requirement)
○ implement some tests
(we not expect 100% code coverage, but test the most complex / heavy part)
○ choose a CLI Framework of your choice
○ find a YAML config library
○ think about logging and error handling
○ feel free to extend this sample file / CLI parameters
○ provide a progress bar (requirement)
○ format your code based on Microsoft Code Style / Conventions
○ provide result as a Git (Github, Gitlab, ..) repository (including commit history)
(requirement)
○ TODO comments are totally fine
○ link references / sources / ideas
○ provide small documentation / README file
● Timing:
○ max. 2 days of implementation
○ we are not expecting a feature ready implementation but the general
direction/usage should be there
● Further progress:
○ walk us through your code
○ Q&A about implementation details, the good and bad parts

Yaml configuration file:
config:
  parallel_downloads: 3
  download_dir: ./downloads/linux-images
downloads:
  - url: https://releases.ubuntu.com/20.04.2/ubuntu-20.04.2-live-server-amd64.iso
    file: ubuntu-20.04.2-live-server-amd64.iso
    sha256: d1f2bf834bbe9bb43faf16f9be992a6f3935e65be0edece1dee2aa6eb1767423
    overwrite: true
  - url: https://cdimage.ubuntu.com/releases/20.10/release/ubuntu-20.10-live-server-arm64.iso
    file: ubuntu-20.10-live-server-arm64.iso
    overwrite: false
  - url: http://ftp.halifax.rwth-aachen.de/archlinux/iso/2021.03.01/archlinux-bootstrap-2021.03.01-x86_64.tar.gz
    sha1: f0e9a794dbbc2f593389100273a3714d46c5cecf
    file: archlinux-bootstrap-2021.03.01-x86_64.tar.gz
  - url: https://download.fedoraproject.org/pub/fedora/linux/releases/33/Server/x86_64/iso/Fedora-Server-netinst-x86_64-33-1.2.iso
    file: Fedora-Server-netinst-x86_64-33-1.2.iso
    sha256: 1f1f018e78f0cc23d08db0c85952344ea5c200e67b672da5b07507c066a52ccf
  - url: https://cdimage.debian.org/debian-cd/current/amd64/iso-cd/debian-10.8.0-amd64-netinst.iso
    file: debian-10.8.0-amd64-netinst.iso
    sha256: 396553f005ad9f86a51e246b1540c60cef676f9c71b95e22b753faef59f89bee