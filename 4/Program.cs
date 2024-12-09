using System.Text;
using Renci.SshNet;

var connectionInfo =
    new ConnectionInfo("localhost", "keras_user", new PasswordAuthenticationMethod("keras_user", "password"));
using var ssh = new SshClient(connectionInfo);
using var scp = new ScpClient(connectionInfo);

ssh.Connect();
scp.Connect();

if (File.Exists("filter_model.keras"))
    scp.Upload(new FileInfo("filter_model.keras"), "/home/keras_user/filter_model.keras");
scp.Upload(new FileInfo("script.py"), "/home/keras_user/script.py");

await using var shell = ssh.CreateShellStream("kraken", 80, 24, 800, 600, 1024);
shell.DataReceived += (_, args) => Console.Write(Encoding.UTF8.GetString(args.Data));
shell.WriteLine("python3 script.py");
shell.WriteLine("exit");
shell.Expect("logout");

string[] resultFiles = ["filter_model.keras", "model.png", "result.png"];
foreach (var file in resultFiles)
    scp.Download(file, new FileInfo(file));