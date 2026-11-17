[CmdletBinding()]
param(
    [string]$BaseUrl = 'http://localhost:5074',
    [Parameter(Mandatory = $true)][string]$AdminEmail,
    [Parameter(Mandatory = $true)][string]$AdminPassword
)

$ErrorActionPreference = 'Stop'
$api = $BaseUrl.TrimEnd('/') + '/api/v1'
$suffix = [Guid]::NewGuid().ToString('N')
$password = '1234'

function Invoke-Api([string]$method, [string]$path, [object]$body, [string]$token) {
    $headers = @{}
    if ($token) { $headers.Authorization = "Bearer $token" }
    $parameters = @{ Method = $method; Uri = "$api$path"; Headers = $headers; SkipHttpErrorCheck = $true }
    if ($null -ne $body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $body | ConvertTo-Json -Depth 10
    }
    $response = Invoke-WebRequest @parameters
    $json = if ($response.Content) { $response.Content | ConvertFrom-Json } else { $null }
    [PSCustomObject]@{ Status = [int]$response.StatusCode; Body = $json }
}

$health = Invoke-WebRequest -Method Get -Uri ($BaseUrl.TrimEnd('/') + '/health') -SkipHttpErrorCheck
$unauthorized = Invoke-Api 'GET' '/demandas' $null $null

$userAEmail = "jornada-a-$suffix@example.invalid"
$userBEmail = "jornada-b-$suffix@example.invalid"
$registerA = Invoke-Api 'POST' '/auth/register' @{ email = $userAEmail; alias = 'Moradora A'; password = $password; role = 'Admin' } $null
$registerB = Invoke-Api 'POST' '/auth/register' @{ email = $userBEmail; alias = 'Morador B'; password = $password } $null
$loginA = Invoke-Api 'POST' '/auth/login' @{ email = $userAEmail; password = $password } $null
$loginB = Invoke-Api 'POST' '/auth/login' @{ email = $userBEmail; password = $password } $null
$loginAdmin = Invoke-Api 'POST' '/auth/login' @{ email = $AdminEmail; password = $AdminPassword } $null

$demand = Invoke-Api 'POST' '/demandas' @{
    descricao = 'Entulho bloqueia a calçada e dificulta a passagem.'
    local = 'Em frente à entrada da praça, na Rua Exemplo.'
} $loginA.Body.accessToken
$demandId = $demand.Body.demanda.id

$forbiddenAdminPatch = Invoke-Api 'PATCH' "/demandas/$demandId" @{ categoria = 'Fisica' } $loginA.Body.accessToken
$otherAuthorUpdate = Invoke-Api 'PUT' "/demandas/$demandId" @{
    descricao = 'Tentativa de alteração por outro participante.'
    local = 'Outro local informado pelo participante.'
} $loginB.Body.accessToken

$support1 = Invoke-Api 'PUT' "/demandas/$demandId/apoio" $null $loginB.Body.accessToken
$support2 = Invoke-Api 'PUT' "/demandas/$demandId/apoio" $null $loginB.Body.accessToken
$comment = Invoke-Api 'POST' "/demandas/$demandId/comentarios" @{
    texto = 'Sugiro retirar o material e liberar a faixa de circulação.'
} $loginB.Body.accessToken

$classified = Invoke-Api 'PATCH' "/demandas/$demandId" @{ categoria = 'Fisica' } $loginAdmin.Body.accessToken
$progress = Invoke-Api 'PATCH' "/demandas/$demandId" @{
    situacao = 'EmAndamento'
    justificativa = 'A demanda está em análise pela administração.'
} $loginAdmin.Body.accessToken
$concluded = Invoke-Api 'PATCH' "/demandas/$demandId" @{
    situacao = 'Concluida'
    justificativa = 'O material foi retirado e a passagem ficou livre.'
} $loginAdmin.Body.accessToken

$afterConclusion = Invoke-Api 'POST' "/demandas/$demandId/comentarios" @{
    texto = 'A comunidade pode acompanhar o resultado informado.'
} $loginA.Body.accessToken
$moderated = Invoke-Api 'PATCH' "/comentarios/$($comment.Body.id)/moderacao" @{
    motivo = 'Comentário ocultado para verificar a moderação administrativa.'
} $loginAdmin.Body.accessToken
$detail = Invoke-Api 'GET' "/demandas/$demandId" $null $loginA.Body.accessToken
$visibleComments = Invoke-Api 'GET' "/demandas/$demandId/comentarios" $null $loginA.Body.accessToken

[ordered]@{
    healthStatus = [int]$health.StatusCode
    publicRegistrationRole = $registerA.Body.role
    unauthorizedStatus = $unauthorized.Status
    forbiddenAdminPatchStatus = $forbiddenAdminPatch.Status
    otherAuthorUpdateStatus = $otherAuthorUpdate.Status
    demandCreateStatus = $demand.Status
    supportStatuses = @($support1.Status, $support2.Status)
    supportCountAfterDuplicate = $detail.Body.demanda.totalApoios
    commentCreateStatus = $comment.Status
    classifyStatus = $classified.Status
    progressStatus = $progress.Status
    concludeStatus = $concluded.Status
    historyCount = $detail.Body.historico.Count
    commentAfterConclusionStatus = $afterConclusion.Status
    moderationStatus = $moderated.Status
    visibleCommentCount = $visibleComments.Body.totalItems
} | ConvertTo-Json -Depth 5
