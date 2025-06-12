using System;
using System.Collections.Generic;
using System.Linq;

namespace SubnetCalculator
{
    class Program
    {
        static void Main(string[] args)
        {
            bool continueProgram = true;

            while (continueProgram)
            {
                Console.Clear();
                Console.WriteLine("Subnetting Tool");
                Console.WriteLine("---------------");
                Console.WriteLine("1. Calcular una subred (CIDR)");
                Console.WriteLine("2. Calcular subredes con VLSM");
                Console.WriteLine("0. Salir");
                Console.Write("Seleccione una opción: ");
                string option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        CalculateSingleSubnet(); // Option 1: Simple subnet with IP + CIDR or IP + host count
                        break;
                    case "2":
                        RunVLSMCalculator(); // Option 2: VLSM calculation with varying subnet sizes
                        break;
                    case "0":
                        continueProgram = false;
                        break;
                    default:
                        Console.WriteLine("Opción inválida. Intente de nuevo.");
                        break;
                }

                if (continueProgram)
                {
                    Console.WriteLine("\nPresione cualquier tecla para continuar...");
                    Console.ReadKey();
                }
            }
        }

        // Opción 1: Cálculo de una sola subred usando CIDR o número de hosts
        static void CalculateSingleSubnet()
        {
            Console.WriteLine("\nSeleccione el modo de cálculo:");
            Console.WriteLine("1. Ingresar IP con CIDR (ej. 192.168.1.0/24)");
            Console.WriteLine("2. Ingresar IP base y cantidad de hosts");
            Console.Write("Opción: ");
            string mode = Console.ReadLine().Trim();

            string ip;
            int cidr = -1;

            if (mode == "1")
            {
                // Leer IP/CIDR
                Console.Write("Ingrese la dirección IP con CIDR (ej. 192.168.1.0/24): ");
                string input = Console.ReadLine();
                string[] parts = input.Split('/');

                if (parts.Length != 2)
                {
                    Console.WriteLine("Formato inválido. Use IP/CIDR (ej. 192.168.1.0/24).");
                    return;
                }

                ip = parts[0].Trim();

                if (!int.TryParse(parts[1], out cidr) || cidr < 0 || cidr > 32)
                {
                    Console.WriteLine("CIDR inválido. Debe estar entre 0 y 32.");
                    return;
                }
            }
            else if (mode == "2")
            {
                // Leer IP base y número de hosts deseados
                Console.Write("Ingrese la dirección IP base (ej. 192.168.1.0): ");
                ip = Console.ReadLine().Trim();

                Console.Write("Ingrese la cantidad de hosts requeridos: ");
                if (!int.TryParse(Console.ReadLine(), out int hostsNeeded) || hostsNeeded <= 0)
                {
                    Console.WriteLine("Cantidad inválida de hosts.");
                    return;
                }

                // Calcular CIDR requerido para los hosts
                int bitsNeeded = (int)Math.Ceiling(Math.Log(hostsNeeded + 2, 2));
                cidr = 32 - bitsNeeded;
            }
            else
            {
                Console.WriteLine("Opción inválida.");
                return;
            }

            try
            {
                string binaryIp = EncodeIP(ip);                         // Convertir IP a binario
                string network = GetNetworkAddress(binaryIp, cidr);     // Calcular dirección de red
                string broadcast = GetBroadcastAddress(binaryIp, cidr); // Calcular broadcast
                int totalHosts = GetTotalHosts(network, broadcast);     // Total de IPs
                int usableHosts = GetUsableHosts(totalHosts);           // IPs usables
                string usableRange = GetUsableIPRange(usableHosts, network, broadcast); // Rango de IPs válidas
                string mask = CidrToMask(cidr);                         // Convertir CIDR a máscara decimal

                // Mostrar resultados
                Console.WriteLine("\n----- Resultados -----");
                Console.WriteLine($"Dirección de red      : {network}");
                Console.WriteLine($"Máscara de subred     : {mask}");
                Console.WriteLine($"CIDR                  : /{cidr}");
                Console.WriteLine($"Dirección broadcast   : {broadcast}");
                Console.WriteLine($"Rango de IPs válidas  : {usableRange}");
                Console.WriteLine($"Cantidad total de IPs : {totalHosts}");
                Console.WriteLine($"Cantidad de hosts útiles : {usableHosts}");
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        // Opción 2: Cálculo de subredes VLSM
        static void RunVLSMCalculator()
        {
            Console.WriteLine("\nVLSM Subnet Calculator");
            Console.WriteLine("----------------------");

            Console.Write("Ingrese la dirección de red (ej. 192.168.1.0): ");
            string ipBase = Console.ReadLine().Trim();

            Console.Write("Ingrese el número de subredes: ");
            if (!int.TryParse(Console.ReadLine(), out int numberOfSubnets) || numberOfSubnets <= 0)
            {
                Console.WriteLine("Número de subredes inválido. Ingrese un número mayor a 0.");
                return;
            }

            List<Host> hosts = new List<Host>();
            for (int i = 0; i < numberOfSubnets; i++)
            {
                Console.Write($"Ingrese la cantidad de hosts para la subred {i + 1}: ");
                if (!int.TryParse(Console.ReadLine(), out int hostsNeeded) || hostsNeeded <= 0)
                {
                    Console.WriteLine("Cantidad de hosts inválida. Ingrese un número mayor a 0.");
                    return;
                }
                hosts.Add(new Host { Name = $"Subred {i + 1}", Number = hostsNeeded });
            }

            var subnets = GenerateSubnets(ipBase, hosts);

            string resultText = "\nResultados de las Subredes:\n";
            foreach (var subnet in subnets)
            {
                resultText += $"Subred: {subnet.Name}\n";
                resultText += $"   Dirección de Red: {subnet.NetworkIP}\n";
                resultText += $"   Máscara de Subred: {subnet.Mask}\n";
                resultText += $"   CIDR: /{subnet.CIDR}\n";
                resultText += $"   Hosts Necesarios: {subnet.HostsNeeded}\n";
                resultText += $"   Hosts Disponibles: {subnet.AvailableHosts}\n";
                resultText += $"   Rango Utilizable: {subnet.UsableRange}\n";
                resultText += $"   Dirección de Broadcast: {subnet.Broadcast}\n\n";
            }

            Console.WriteLine(resultText);
            ExportResults(resultText);
        }

        // Convierte dirección IP a uint (para operaciones aritméticas)
        static uint IPToUInt32(string ipAddress)
        {
            string[] octets = ipAddress.Split('.');
            uint result = 0;
            for (int i = 0; i < 4; i++)
            {
                result |= (uint)(int.Parse(octets[i]) << ((3 - i) * 8));
            }
            return result;
        }

        // Lógica principal para generación de subredes VLSM
        static List<Subnet> GenerateSubnets(string ipBase, List<Host> hosts)
        {
            List<Subnet> subnets = new List<Subnet>();
            string currentNetwork = ipBase;

            // Ordenar de mayor a menor para evitar desperdicio de IPs
            hosts = hosts.OrderByDescending(h => h.Number).ToList();

            foreach (var host in hosts)
            {
                int bitsNeeded = (int)Math.Ceiling(Math.Log(host.Number + 2, 2));
                int subnetCidr = 32 - bitsNeeded;

                int availableHosts = (int)Math.Pow(2, 32 - subnetCidr) - 2;
                if (host.Number > availableHosts)
                {
                    Console.WriteLine($"No se puede asignar {host.Number} hosts a la subred {host.Name}.");
                    return null;
                }

                var subnet = CalculateSubnet(currentNetwork, subnetCidr, host, CidrToMask(subnetCidr));
                subnets.Add(subnet);
                currentNetwork = subnet.NextNetwork; // Avanzar a la siguiente subred
            }

            return subnets;
        }

        // Calcula una subred específica
        static Subnet CalculateSubnet(string network, int cidrMask, Host host, string subnetMask)
        {
            string encodedIP = EncodeIP(network);
            string networkAddress = GetNetworkAddress(encodedIP, cidrMask);
            string broadcastAddress = GetBroadcastAddress(encodedIP, cidrMask);
            int totalHosts = GetTotalHosts(networkAddress, broadcastAddress);
            int usableHosts = GetUsableHosts(totalHosts);
            string usableRange = GetUsableIPRange(usableHosts, networkAddress, broadcastAddress);
            string nextNetwork = GetNextSubnet(broadcastAddress);

            return new Subnet
            {
                Name = host.Name,
                HostsNeeded = host.Number,
                AvailableHosts = usableHosts,
                UsableRange = usableRange,
                Broadcast = broadcastAddress,
                NextNetwork = nextNetwork,
                NetworkIP = networkAddress,
                Mask = subnetMask,
                CIDR = cidrMask
            };
        }

        // Utilidades IP
        static string EncodeIP(string ip)
        {
            var parts = ip.Split('.');
            if (parts.Length != 4 || parts.Any(part => !int.TryParse(part, out int n) || n < 0 || n > 255))
                throw new FormatException("Formato de dirección IP no válido.");
            return string.Join("", parts.Select(octet => Convert.ToString(int.Parse(octet), 2).PadLeft(8, '0')));
        }

        static string DecodeIP(string binary)
        {
            return string.Join(".", Enumerable.Range(0, 4)
                .Select(i => Convert.ToInt32(binary.Substring(i * 8, 8), 2).ToString()));
        }

        static string GetNetworkAddress(string encodedIP, int cidrMask)
        {
            return DecodeIP(string.Join("", encodedIP.Take(cidrMask).Concat(Enumerable.Repeat('0', 32 - cidrMask))));
        }

        static string GetBroadcastAddress(string encodedIP, int cidrMask)
        {
            return DecodeIP(string.Join("", encodedIP.Take(cidrMask).Concat(Enumerable.Repeat('1', 32 - cidrMask))));
        }

        static int GetTotalHosts(string networkAddress, string broadcastAddress)
        {
            long net = Convert.ToInt64(EncodeIP(networkAddress), 2);
            long bc = Convert.ToInt64(EncodeIP(broadcastAddress), 2);
            return (int)(bc - net + 1);
        }

        static int GetUsableHosts(int totalHosts)
        {
            return totalHosts <= 2 ? 0 : totalHosts - 2;
        }

        static string GetUsableIPRange(int usableHosts, string networkAddress, string broadcastAddress)
        {
            if (usableHosts == 0) return "Ninguno";
            long start = Convert.ToInt64(EncodeIP(networkAddress), 2) + 1;
            long end = Convert.ToInt64(EncodeIP(broadcastAddress), 2) - 1;
            return $"{DecodeIP(Convert.ToString(start, 2).PadLeft(32, '0'))} - {DecodeIP(Convert.ToString(end, 2).PadLeft(32, '0'))}";
        }

        static string GetNextSubnet(string broadcastAddress)
        {
            long next = Convert.ToInt64(EncodeIP(broadcastAddress), 2) + 1;
            return DecodeIP(Convert.ToString(next, 2).PadLeft(32, '0'));
        }

        static string CidrToMask(int cidr)
        {
            uint mask = cidr == 0 ? 0 : uint.MaxValue << (32 - cidr);
            return DecodeIP(Convert.ToString(mask, 2).PadLeft(32, '0'));
        }

        static int MaskToCidr(string mask)
        {
            var binary = string.Join("", mask.Split('.')
                .Select(octet => Convert.ToString(int.Parse(octet), 2).PadLeft(8, '0')));
            if (!binary.Contains("0") || binary.TrimEnd('0').Contains("0"))
                throw new FormatException("Máscara no válida.");
            return binary.Count(c => c == '1');
        }

        // Exporta resultados a un archivo .txt
        static void ExportResults(string resultText)
        {
            string fileName = "resultados_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
            try
            {
                System.IO.File.WriteAllText(fileName, resultText);
                Console.WriteLine($"Resultados guardados en {fileName}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar los resultados: {ex.Message}");
            }
        }

        // Modelo de entrada: cada subred con nombre y cantidad requerida
        public class Host
        {
            public string Name { get; set; }
            public int Number { get; set; }
        }

        // Modelo de salida: datos calculados de cada subred
        public class Subnet
        {
            public string Name { get; set; }
            public int HostsNeeded { get; set; }
            public int AvailableHosts { get; set; }
            public string UsableRange { get; set; }
            public string Broadcast { get; set; }
            public string NextNetwork { get; set; }
            public string NetworkIP { get; set; }
            public string Mask { get; set; }
            public int CIDR { get; set; }
        }
    }
}
