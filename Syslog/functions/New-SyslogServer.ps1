function New-SyslogServer
{
<#
	.SYNOPSIS
		Creates a new syslog server.
	
	.DESCRIPTION
		Creates a new syslog server.
		This server will accept incoming messages, transform them and then pass them on using Syslog.
		All connections expected to be TCP
	
	.PARAMETER RegexReplacements
		Regex replacements to execute on incoming messages.
		Provide a set of patterns and their replacements, where the patterns are the keys and the replacements are the values.
		Provide an empty hashtable to pass through messages unaltered.
	
	.PARAMETER RegexOptions
		Which regex options to apply to the replace operations.
		Defaults to: "IgnoreCase"
	
	.PARAMETER InPort
		On which port to receive messages.
		Defaults to 514
	
	.PARAMETER ListenOn
		On which IP Address to listen for incoming messages.
		Defaults to "Any"
	
	.PARAMETER OutServer
		To which server to forward messages to.
	
	.PARAMETER OutPort
		On which port on the destination server to connect to.
		Defaults to 514
	
	.PARAMETER WorkerCount
		How many parallel workers to launch for processing and forwarding messages.
		Defaults to 1
	
	.PARAMETER Start
		Start the server after creating it.
	
	.EXAMPLE
		PS C:\> New-SyslogServer -RegexReplacements @{ 'Fabrikam\.org' = 'contoso.com' } -OutServer 'syslog.contoso.com'
	
		Creates a new syslog server that - once started - will pass through messages to syslog.contoso.com, replacing all instances of "Fabrikam.org" with "contoso.com"
#>
	[Diagnostics.CodeAnalysis.SuppressMessageAttribute("PSUseShouldProcessForStateChangingFunctions", "")]
	[OutputType([Syslog.Server])]
	[CmdletBinding(DefaultParameterSetName = 'RegexWorker')]
	param (
		[Parameter(Mandatory = $true, ParameterSetName = 'RegexWorker')]
		[hashtable]
		$RegexReplacements,
		
		[Parameter(ParameterSetName = 'RegexWorker')]
		[System.Text.RegularExpressions.RegexOptions]
		$RegexOptions = [System.Text.RegularExpressions.RegexOptions]::IgnoreCase,
		
		[int]
		$InPort = 514,
		
		[System.Net.IPAddress]
		$ListenOn = [ipaddress]::Any,
		
		[Parameter(Mandatory = $true)]
		[string]
		$OutServer,
		
		[int]
		$OutPort = 514,
		
		[int]
		$WorkerCount = 1,
		
		[switch]
		$Start
	)
	
	begin
	{
		$parameters = [System.Collections.Generic.Dictionary[string, object]]::new()
		
		if ($RegexReplacements) {
			$kind = 'Regex'
			foreach ($key in $RegexReplacements.Keys) {
				$parameters[$key] = $RegexReplacements[$key]
			}
			$parameters['RegexOptions'] = $RegexOptions
		}
	}
	process
	{
		$serverObject = [Syslog.Server]::new($WorkerCount, $InPort, $ListenOn, $OutPort, $OutServer, $kind, $parameters)
		$script:servers += $serverObject
		if ($Start) { $serverObject.Start() }
		$serverObject
	}
}