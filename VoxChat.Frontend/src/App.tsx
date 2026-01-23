import  WaitingRoom  from './components/WaitingRoom.tsx';
import {HubConnection, HubConnectionBuilder} from "@microsoft/signalr";
import {useState} from "react";
import {Chat} from "@/components/Chat.tsx";
interface MessageObj {
    username: string;
    content: string;
}
export type {MessageObj}
export type sendPeer = (peerId: string) => Promise<void>;
export type PeersIdList = string[];

function App() {

    const [connection, setConnection] = useState<HubConnection | null>(null);
    const [chatroom, setChatroom] = useState("");
    const [peersId, setPeersId] = useState<PeersIdList>([]);

    const [messages, setMessages] = useState<MessageObj[]>([]);
    const [chatMembers, setChatMembers] = useState<string[]>([]);

    async function joinChat(username: string, chatroom: string ): Promise<void> {

        const connection = new HubConnectionBuilder()
            .withUrl("http://localhost:5274/chat")
            .withAutomaticReconnect()
            .build();

        connection.on("ReceiveMessage", (username, message) => {
            setMessages(messages => [... messages, {username, content: message}]);
        });

        connection.on("GetChatLog", (chatLog) => {
            setMessages(chatLog);

        } );
        connection.on("ReceivePeer", (peerId) => {
            console.log("Got: " + peerId);
            setPeersId(peerId);
        })
        connection.on("GetChatMembersList", (membersList) => {
            setChatMembers(membersList);
            console.log("chat members: " + membersList);
        } )




        setConnection(connection);
        setChatroom(chatroom);


        try{
            await connection.start();
            await connection.invoke("JoinChat", {username, chatroom});
            console.log(connection)

        } catch (e) {

            if (e instanceof (Error))
                console.log(e.message);

        }

    }

    function sendMessage(message: string) {
        connection?.invoke("SendMessage", message);
    }

    async function sendPeer(peerId: string) {
        console.log("Sending Peer", peerId);

        while (connection?.state !== "Connected")
            await new Promise(r => setTimeout(r, 200));

        await connection?.invoke("SendPeer", peerId);
        console.log("sent peerId end");
    }

    async function closeChat(){
        await connection?.stop();
        setConnection(null);
        setMessages([]);
        setPeersId([])
        setChatMembers([])
    }

    return (
        <div>
            {connection ?
                <Chat messages={messages}
                      chatroom={chatroom}
                      sendMessage={sendMessage}
                      closeButton={closeChat}
                      sendPeer={sendPeer}
                      peersId={peersId}
                      chatMembers={chatMembers}/>
                :
                <WaitingRoom joinChat={joinChat}/>}
        </div>
    );
}

export default App
