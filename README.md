Virtual Stores Price Tracker

Este projeto é um rastreador de preços de produtos de lojas como Amazon, Kabum, Steam, PS Store, entre outros, desenvolvido em C# e utilizando o Playwright. Ele verifica se o preço atual de uma lista de produtos está abaixo de um determinado valor e, se estiver, envia um e-mail de alerta para uma lista de destinatários. O processo é repetido a cada 30 minutos.

Requisitos

    .NET 6.0 ou superior
    Playwright para .NET

Instalação

    Clone o repositório ou baixe o projeto para sua máquina local.
    No diretório do projeto, execute dotnet restore para instalar as dependências.
    Crie um arquivo chamado email_credentials.json no diretório raiz do projeto com as credenciais do e-mail que será utilizado para enviar as notificações (exemplo de conteúdo abaixo):
    Atualmente só é suportado e-mails do Hotmail

{

  "Email": "seu_email@example.com",
  
  "Password": "sua_senha"
  
}

    Crie um arquivo chamado email_recipients.txt no diretório raiz do projeto contendo uma lista de e-mails que receberão as notificações. Coloque um e-mail por linha, como no exemplo abaixo:

destinatario1@example.com

destinatario2@example.com

    Crie um arquivo chamado products.txt no diretório raiz do projeto contendo uma lista de URLs dos produtos que você deseja rastrear e os respectivos preços-alvo. Coloque um produto por linha, separando a URL e o preço-alvo por vírgula, como no exemplo abaixo:

https://www.amazon.com.br/Produto1/dp/XXXXX, 100

https://www.amazon.com.br/Produto2/dp/XXXXX, 150

https://store.steampowered.com/app/1364780/Street_Fighter_6/, 90

https://store.playstation.com/pt-br/product/UP0102-PPSA02632_00-SF600000STANDARD, 90

    Compile e execute o projeto com dotnet build e dotnet run, respectivamente.

Instalando o navegador do Playwright

    Faça o build da solução.
    Abra o Powershell 6.0 ou superior.
    Navegue até a pasta de compilação do projeto. Substitua <PATH_TO_BUILD_FOLDER> pelo caminho correto da pasta de compilação no seu computador.
    cd "<PATH_TO_BUILD_FOLDER>"
    Exemplo:

    cd "C:\AmazonPriceTracker\AmazonPriceTracker\bin\Debug\net6.0"

    Execute o comando a seguir para instalar o navegador do Playwright:

    pwsh playwright.ps1 install

Após seguir estas etapas, o navegador do Playwright estará instalado e pronto para ser usado no projeto.

Pelo projeto compilado

https://github.com/alanspindler/AmazonPriceTracker/blob/main/AmazonPriceTracker/Projecto%20Compilado.zip

    Necessita do PowerShell 6.0

    Descompacte o arquivo Projeto Compilado.zip.
    Edite o arquivo email_credentials.json com suas credenciais de e-mail do hotmail (usuário e senha)
    Edite o arquivo email_recipients.txt com a lista de e-mails para os quais deseja enviar.
    Edite o arquivo products.txt com os produtos que deseja verificar e o preço de alerta.
    Execute o arquivo Instalar Playwright.bat.
    Aguarde a instalação finalizar.
    Execute o arquivo AmazonPriceTracker.exe.

Funcionalidades

    Rastrear preços de produtos da Amazon, Kabum, Steam e PS Store.
    Enviar e-mails de alerta quando o preço do produto estiver abaixo do valor desejado.    
    Ler a lista de produtos e preços-alvo de um arquivo externo.
    Ler a lista de destinatários de e-mail de um arquivo externo.
    Utilizar credenciais de e-mail de um arquivo externo para maior segurança.

Limitações

    A implementação atual é específica para a lojas do Brasil.    

Contribuindo

Sinta-se à vontade para contribuir com melhorias ou correções de bugs. Faça um fork do projeto, crie uma branch, realize as alterações e envie um pull request.
Licença

Este projeto é licenciado sob a MIT License. Consulte o arquivo LICENSE.md para mais detalhes.
