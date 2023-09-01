/* Sample Codebase that implements the integration from the documentation https://github.com/NamlogInnovation/documentation
 * 
 * 
 * 
 * 
 * 
 * (C) Namlog 2023
*/



using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationSample
{
   

    internal class Program
    {
        static async Task Main(string[] args)
        {
            string baseUri = "https://fmsintegrations.namlog.co.za/api/v1";
            string getRoutesEndpoint = "/Integrations/client/{integrationClientId}/routes";
            string createWaybillEndpoint = "/Integrations/waybill";
            string getNonBillingCustomersEndpoint = "/Integrations/{billingCustomerId}/nonbillcustomers";

            //Test account credentials. These have a expiration date, and if you require testing access, please refer to your contact accordingly 
            string clientId = "@N@ml0gt3stuser$$";
            string clientSecret = "7089B688-56D0-42A5-A60F-744C8D8A2360";
            int integrationClientId = 1;  // Example test integration client ID


            HttpClient httpClient = new HttpClient();
            // Set authorization headers
            httpClient.DefaultRequestHeaders.Add("ClientId", clientId);
            httpClient.DefaultRequestHeaders.Add("ClientSecret", clientSecret);

            // Set content type header
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Get routes for a consignment
            HttpResponseMessage routesResponse = await httpClient.GetAsync($"{baseUri}{getRoutesEndpoint}".Replace("{integrationClientId}", integrationClientId.ToString()));

            Route selectedRoute = new Route();
            Customer pickupCustomer = new Customer();
            Customer deliveryCustomer = new Customer();

            if (routesResponse.IsSuccessStatusCode)
            {
                string routesData = await routesResponse.Content.ReadAsStringAsync();
                // Process and use routes data as needed
                //Handle your exceptions here ...

                Routes waybillRoutes = JsonSerializer.Deserialize<Routes>(routesData);

                foreach (var route in waybillRoutes.data)
                {
                    if (route.routeId == 7941) // This id 7941 is from our pre-chosen route, based on the fromTown (Johannesburg), and toTown (UPINGTON)
                    {
                        selectedRoute = route;
                        break;
                    }
                }

                if (selectedRoute == null)
                    return;


                // Get routes for a consignment
                HttpResponseMessage customersResponse = await httpClient.GetAsync($"{baseUri}{getNonBillingCustomersEndpoint}".Replace("{billingCustomerId}", selectedRoute.billCustomerId.ToString()));

                if(customersResponse != null && customersResponse.IsSuccessStatusCode)
                {
                    string customersResponseData = await customersResponse.Content.ReadAsStringAsync();
                    //Handle your exceptions here ...


                    Customers customers = JsonSerializer.Deserialize<Customers>(customersResponseData);

                    /// Match the route with the pickup and delivery customers using the bcentre info
                    /// FromTown 
                    /// ToTown
                    /// This loop just demonstrates how these values are compared once received.
                    foreach (var cust in customers.data)
                    {
                        if (selectedRoute.fromPCode.Trim().Equals(cust.bcentreId.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            Console.WriteLine($"{cust.name} : pickup customer Id is:  {cust.customerId} ");

                        if (selectedRoute.fromPCode.Trim().Equals(cust.bcentreId.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            if (selectedRoute.fromTown.Trim().Equals(cust.bcentre.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                Console.WriteLine($"{cust.name} : pickup customer Id is:  {cust.customerId} ");
                                pickupCustomer = cust;
                            }

                        if (selectedRoute.toPCode.Trim().Equals(cust.bcentreId.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            if (selectedRoute.toTown.Trim().Equals(cust.bcentre.Trim(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                Console.WriteLine($"Matching customer {cust.name} , {cust.displayText}  : delivery customer Id is:   {cust.customerId} ");
                                deliveryCustomer = cust;
                            }
                    }

                    int preSelectedCustomerId = 196383; // Your chosen pick up customer

                    pickupCustomer = customers.data.FirstOrDefault(c => c.customerId == preSelectedCustomerId);
                    deliveryCustomer = customers.data.FirstOrDefault(c => c.bcentreId == selectedRoute.toPCode 
                                                                    && c.bcentre.Trim() == selectedRoute.toTown.Trim());

                }
            }
            else
            {
                Console.WriteLine("Error fetching routes: " + await routesResponse.Content.ReadAsStringAsync());
            }

            Random random = new Random();   
            int wbNumbner =  random.Next();


            //Assign your waybill number and barcode, these need to be unique
            string waybillNumber = $"TESTAPI_{wbNumbner}";
            string barcode = $"TESTBARCODE{wbNumbner}";
            int parcelDefaultWeight = 5; 

            // Create a waybill
            var waybillPayload = new
            {
                integrationModel = new
                {
                    cService = selectedRoute.cService,
                    courierId = selectedRoute.courierId,
                    depotId = 1,
                    billCustomerId = selectedRoute.billCustomerId,
                    pickupCustomerId = pickupCustomer.customerId,
                    integrationClientId = integrationClientId, 
                    customers = new List<WaybillCustomer>
        {
            new WaybillCustomer
            {
                customerName = deliveryCustomer.name,
                custRef = "",
                newCustReference = "",
                routeId = selectedRoute.routeId,
                billCustomerId = selectedRoute.billCustomerId,
                address = new Address
                {
                    town =  deliveryCustomer.bcentre,
                    postalCode = deliveryCustomer.bcentreId, 
                },
                contact = new Contact
                {
                    email = ""
                },
                waybills = new List<Waybill>
                {
                    new Waybill
                    {
                        wayBillNo = waybillNumber, 
                        sendingCustomer = selectedRoute.billCustomer, 
                        receivingCustomer = deliveryCustomer.name,
                        routeId = selectedRoute.routeId,
                        billCust = selectedRoute.billCustomerId,
                        cService = selectedRoute.cService,
                        courierId = selectedRoute.courierId,
                        depotId = 1,
                        billCustomerId = selectedRoute.billCustomerId, 
                        pickupCustomerId = pickupCustomer.customerId,
                        noOfParcels = 1,
                        parcels = new List<Parcel>
                        {
                            new Parcel
                            {
                                parcelTypeId = 0,
                                parcelType = "PARCEL",
                                dimms = 0,
                                barcode = barcode, 
                                orderId = "",
                                weight = parcelDefaultWeight,
                                pl = 0,
                                ph = 0,
                                pw = 0,
                                verificationTypeId = 0
                            }
                        },
                        orders = new List<Order>
                        {
                            new Order
                            {
                                waybillNo = waybillNumber, 
                                orderNo = "",
                                invoiceNo = ""
                            }
                        }
                    }
                }
            }
        },
                    branch = 0
                }
            };


            HttpResponseMessage createWaybillResponse = await httpClient.PostAsJsonAsync($"{baseUri}{createWaybillEndpoint}", waybillPayload);
            if (createWaybillResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Waybill created successfully.");
            }
            else
            {
                Console.WriteLine("Error creating waybill: " + await createWaybillResponse.Content.ReadAsStringAsync());
            }
        }
    }


    #region Refactor classes accordingly 

    public class Route
    {
        public int billCustomerId { get; set; }
        public string billCustomer { get; set; }
        public int routeId { get; set; }
        public bool activeFlag { get; set; }
        public string fromTown { get; set; }
        public string fromPCode { get; set; }
        public string toTown { get; set; }
        public string toPCode { get; set; }
        public string courierId { get; set; }
        public string cService { get; set; }
    }

    public class Routes
    {
        public bool succeeded { get; set; }
        public List<Route> data { get; set; }
    }

    public class CustomerAddress
    {
        public int customerAddressId { get; set; }
        public double lat { get; set; }
        public double @long { get; set; }
        public int suburbId { get; set; }
        public int townId { get; set; }
        public int provinceId { get; set; }
        public int countryId { get; set; }
        public string postalCode { get; set; }
        public int customerId { get; set; }
        public int customerAddressTypeId { get; set; }
        public bool verified { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string address3 { get; set; }
        public string address4 { get; set; }
        public int custMainId { get; set; }
        public bool? activeFlag { get; set; }
    }

    public class Customer
    {
        public int customerId { get; set; }
        public string name { get; set; }
        public int custMainId { get; set; }
        public int pcentreId { get; set; }
        public string vatno { get; set; }
        public string importVatno { get; set; }
        public string exportCode { get; set; }
        public string glaccNo { get; set; }
        public bool updCust { get; set; }
        public bool codflag { get; set; }
        public DateTime createDate { get; set; }
        public DateTime lastModified { get; set; }
        public string bcentreId { get; set; }
        public bool isBillCustomer { get; set; }
        public bool sendingBranch { get; set; }
        public int customerStatusId { get; set; }
        public string bcentre { get; set; }
        public string displayText { get; set; }
        public int customerAddressId { get; set; }
        public int suburbId { get; set; }
        public int townId { get; set; }
        public int provinceId { get; set; }
        public int countryId { get; set; }
        public int customerAddressTypeId { get; set; }
        public int custProvinceID { get; set; }
        public CustomerAddress customerAddress { get; set; }
        public int? createUser { get; set; }
        public int? lastModifiedUserId { get; set; }
    }


    public class Customers
    {
        public bool succeeded { get; set; }
        public List<Customer> data { get; set; }
    }


    public class Address
    {
        public string address1 { get; set; }
        public string streetAddress { get; set; }
        public string town { get; set; }
        public string postalCode { get; set; }
    }

    public class Contact
    {
        public string email { get; set; }
    }


    public class IntegrationModel
    {
        public string cService { get; set; }
        public string courierId { get; set; }
        public int depotId { get; set; }
        public int billCustomerId { get; set; }
        public int pickupCustomerId { get; set; }
        public int integrationClientId { get; set; }
        public List<WaybillCustomer> customers { get; set; }
        public int branch { get; set; }
    }

    public class Order
    {
        public string waybillNo { get; set; }
        public string orderNo { get; set; }
        public string invoiceNo { get; set; }
    }

    public class Parcel
    {
        public int parcelTypeId { get; set; }
        public string parcelType { get; set; }
        public int dimms { get; set; }
        public string barcode { get; set; }
        public string orderId { get; set; }
        public int weight { get; set; }
        public int pl { get; set; }
        public int ph { get; set; }
        public int pw { get; set; }
        public int verificationTypeId { get; set; }
    }

    public class Root
    {
        public IntegrationModel integrationModel { get; set; }
    }

    public class Waybill
    {
        public string wayBillNo { get; set; }
        public string sendingCustomer { get; set; }
        public string receivingCustomer { get; set; }
        public int routeId { get; set; }
        public string country { get; set; }
        public int billCust { get; set; }
        public string cService { get; set; }
        public string courierId { get; set; }
        public int depotId { get; set; }
        public int billCustomerId { get; set; }
        public int pickupCustomerId { get; set; }
        public int noOfParcels { get; set; }
        public List<Parcel> parcels { get; set; }
        public List<Order> orders { get; set; }
    }



    public class WaybillCustomer
    {
        public string customerName { get; set; }
        public string custRef { get; set; }
        public string newCustReference { get; set; }
        public int routeId { get; set; }
        public int billCustomerId { get; set; }
        public Address address { get; set; }
        public Contact contact { get; set; }
        public List<Waybill> waybills { get; set; }
    }

    #endregion

}