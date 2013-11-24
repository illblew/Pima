Imports System.Net
Imports System.Net.Sockets
Imports System.Threading
Imports System.Text
Imports System.IO

Module modPima
    Public Class pimaServ
        Public Shared Sub Main()
            Try
                Dim Port As String = "80"
                Dim host As String = Dns.GetHostName()
                Dim SIP As IPAddress = Dns.Resolve(host).AddressList(0)
                Dim tSock As New TcpListener(SIP, Int32.Parse(Port))
                tSock.Start()
                Console.WriteLine("Pima Started @ " & SIP.ToString() & " on " & Port)
                Dim session As New HTTPSession(tSock)
                Dim sThread As New Thread(New ThreadStart(AddressOf session.ProcessThread))
                sThread.Start()
            Catch ex As Exception
                Console.WriteLine(ex.StackTrace.ToString())
            End Try
        End Sub
    End Class

    Public Class HTTPSession
        Private tcp As System.Net.Sockets.TcpListener
        Private clientSocket As System.Net.Sockets.Socket

        Public Sub New(ByVal tcpListener As System.Net.Sockets.TcpListener)
            Me.tcp = tcpListener
        End Sub

        Public Sub ProcessThread()
            While (True)
                Try
                    clientSocket = tcp.AcceptSocket()
                    Dim cData As IPEndPoint = CType(clientSocket.RemoteEndPoint, IPEndPoint)
                    Console.WriteLine("Connection: " + cData.Address.ToString() + ":" + cData.Port.ToString())
                    Dim cThread As New Thread(New ThreadStart(AddressOf ProcessRequest)) ' Thread all the things
                    cThread.Start()
                Catch e As Exception
                    Console.WriteLine(e.StackTrace.ToString())
                    If clientSocket.Connected Then
                        clientSocket.Close()
                    End If
                End Try
            End While
        End Sub

        Protected Sub ProcessRequest()
            Dim recvBytes(1024) As Byte
            Dim hRequest As String = Nothing
            Dim bytes As Int32

            Try
                bytes = clientSocket.Receive(recvBytes, 0, clientSocket.Available, SocketFlags.None)
                hRequest = Encoding.ASCII.GetString(recvBytes, 0, bytes)
                Console.WriteLine("HTTP Request: ")
                Console.WriteLine(hRequest)
                Dim RootDirectory As String = Directory.GetCurrentDirectory() & "\www\"
                Dim vHostIndex As String = "index.html" 'Replace with yaml config in the future (all values)
                Dim sArray() As String
                Dim strRequest As String

                sArray = hRequest.Trim.Split(" ")

                If sArray(0).Trim().ToUpper.Equals("GET") Then 'using GET
                    strRequest = sArray(1).Trim

                    If (strRequest.StartsWith("/")) Then
                        strRequest = strRequest.Substring(1)
                    End If

                    If (strRequest.EndsWith("/") Or strRequest.Equals("")) Then
                        strRequest = strRequest & vHostIndex
                    End If
                    strRequest = RootDirectory & strRequest
                    sendHTMLResponse(strRequest)
                Else
                    'not http so derp
                    strRequest = RootDirectory & "Error\" & "400.html"
                    sendHTMLResponse(strRequest)
                End If
                ' catch
            Catch ex As Exception
                Console.WriteLine(ex.StackTrace.ToString())

                If clientSocket.Connected Then
                    clientSocket.Close()
                End If
            End Try
        End Sub


        Private Function gType(ByVal httpRequest As String) As String
            If (httpRequest.EndsWith("html")) Then
                Return "text/html"
            ElseIf (httpRequest.EndsWith("htm")) Then
                Return "text/html"
            ElseIf (httpRequest.EndsWith("txt")) Then
                Return "text/plain"
            ElseIf (httpRequest.EndsWith("gif")) Then
                Return "image/gif"
            ElseIf (httpRequest.EndsWith("jpg")) Then
                Return "image/jpeg"
            ElseIf (httpRequest.EndsWith("jpeg")) Then
                Return "image/jpeg"
            ElseIf (httpRequest.EndsWith("pdf")) Then
                Return "application/pdf"
            ElseIf (httpRequest.EndsWith("pdf")) Then
                Return "application/pdf"
            ElseIf (httpRequest.EndsWith("doc")) Then
                Return "application/msword"
            ElseIf (httpRequest.EndsWith("xls")) Then
                Return "application/vnd.ms-excel"
            ElseIf (httpRequest.EndsWith("ppt")) Then
                Return "application/vnd.ms-powerpoint"
            Else
                Return "text/plain"
            End If
        End Function

        Private Sub sendHTMLResponse(ByVal httpRequest As String)
            Try
                Dim streamReader As StreamReader = New StreamReader(httpRequest)
                Dim strBuff As String = streamReader.ReadToEnd()
                streamReader.Close()
                streamReader = Nothing
                Dim respByte() As Byte = Encoding.ASCII.GetBytes(strBuff)
                Dim htmlHeader As String = _
                    "HTTP/1.0 200 OK" & ControlChars.CrLf & _
                    "Server: Pima 0.1" & ControlChars.CrLf & _
                    "Content-Length: " & respByte.Length & ControlChars.CrLf & _
                    "Content-Type: " & gType(httpRequest) & _
                    ControlChars.CrLf & ControlChars.CrLf
                Dim headerByte() As Byte = Encoding.ASCII.GetBytes(htmlHeader)

                Console.WriteLine("Header: " & ControlChars.CrLf & htmlHeader)
                clientSocket.Send(headerByte, 0, headerByte.Length, SocketFlags.None)
                clientSocket.Send(respByte, 0, respByte.Length, SocketFlags.None) 'PONG
                clientSocket.Shutdown(SocketShutdown.Both) 'close
                clientSocket.Close() 'kill -9

            Catch ex As Exception
                Console.WriteLine(ex.StackTrace.ToString())

                If clientSocket.Connected Then
                    clientSocket.Close()
                End If
            End Try
        End Sub
    End Class

End Module
