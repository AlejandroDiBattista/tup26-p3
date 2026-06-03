using HttpClient client = new();

string get1 = await client.GetStringAsync("http://localhost:5001/contador");

using HttpRequestMessage put = new(HttpMethod.Put, "http://localhost:5001/contador");
string putResponse = await (await client.SendAsync(put)).Content.ReadAsStringAsync();

using HttpRequestMessage delete = new(HttpMethod.Delete, "http://localhost:5001/contador");
string deleteResponse = await (await client.SendAsync(delete)).Content.ReadAsStringAsync();

string get2 = await client.GetStringAsync("http://localhost:5001/contador");

await File.WriteAllTextAsync(
	"/Users/adibattista/Documents/GitHub/tup26-p3/clases/.contador-check.txt",
	$"GET1={get1}{Environment.NewLine}PUT={putResponse}{Environment.NewLine}DELETE={deleteResponse}{Environment.NewLine}GET2={get2}{Environment.NewLine}");
