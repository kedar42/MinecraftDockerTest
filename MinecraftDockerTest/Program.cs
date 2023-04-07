using System.Text;
using Docker.DotNet;
using Docker.DotNet.Models;

/*
 * todo: Add a way to send commands once the server loads
 * todo: Split the working program into multiple classes
 * todo: split the output of server console from output that shows loading (all until "For help, type "help"")
 * todo: add check that checks if the container needs to be created, if its running and if there is a need to update it nad behave acordingly
 */

var dockerClient = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
var createContainerResponse = await dockerClient.Containers.CreateContainerAsync(new CreateContainerParameters
{
    Image = "openjdk:18",
    Name = "my-minecraft-container",
    Tty = true,
    HostConfig = new HostConfig
    {
        PortBindings = new Dictionary<string, IList<PortBinding>>
        {
            {"25565/tcp", new List<PortBinding> {new PortBinding {HostPort = "25565"}}}
        },
        Binds = new List<string>
        {
            "C:/Server:/server",
        }
    },
    WorkingDir = "/server",
    Cmd = new[] {"java", "-Xms1G", "-Xmx2G", "-jar", "/server/server.jar", "nogui"},
});


await dockerClient.Containers.StartContainerAsync(createContainerResponse.ID, new ContainerStartParameters());

CancellationTokenSource cancellation = new CancellationTokenSource();

var logsStream = await dockerClient.Containers.GetContainerLogsAsync(createContainerResponse.ID, true,
    new ContainerLogsParameters
    {
        Follow = true,
        ShowStderr = true,
        ShowStdout = true,
    }, cancellation.Token);


var buffer = new byte[4096];
while (true)
{
    var result = await logsStream.ReadOutputAsync(buffer, 0, buffer.Length, default);
    var line = Encoding.UTF8.GetString(buffer, 0, result.Count);
    if (string.IsNullOrEmpty(line)) continue;
    Console.WriteLine(line.TrimEnd('\0'));
}