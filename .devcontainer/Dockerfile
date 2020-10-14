FROM mcr.microsoft.com/dotnet/core/sdk:3.1-focal

# Mono is required to run pack.ps1, so we install it here.
RUN apt-get -y update && \
    apt-get -y install dirmngr gnupg apt-transport-https ca-certificates && \
    apt-key adv --keyserver hkp://keyserver.ubuntu.com:80 --recv-keys 3FA7E0328081BFF6A14DA29AA6A19B38D3D831EF && \
    sh -c 'echo "deb https://download.mono-project.com/repo/ubuntu stable-focal main" > /etc/apt/sources.list.d/mono-official-stable.list' && \
    apt-get -y update && \
    apt-get -y install mono-complete && \
    apt-get clean && rm -rf /var/lib/apt/lists/
# We can now get Mono itself.
RUN curl -o /usr/local/bin/nuget.exe https://dist.nuget.org/win-x86-commandline/latest/nuget.exe && \
    # Create as alias for nuget
    echo "alias nuget=\"mono /usr/local/bin/nuget.exe\"" >> /root/.bash_aliases
