FROM mcr.microsoft.com/windows/servercore:ltsc2022

USER ContainerAdministrator
ADD https://aka.ms/highdpimfc2013x64enu vc_redist2013.exe
ADD https://aka.ms/vs/16/release/vc_redist.x64.exe vc_redist.exe

RUN vc_redist2013.exe /passive /norestart
RUN vc_redist.exe /passive /norestart
RUN del vc_redist2013.exe && del vc_redist.exe 

USER ContainerUser
COPY . .
ENV TORCH_GAME_PATH="c:\dedi"
ENV TORCH_INSTANCE="c:\instance"
ENV TORCH_SERVICE="true"
ENTRYPOINT ["Torch.Server.exe"]
CMD ["-noupdate"]