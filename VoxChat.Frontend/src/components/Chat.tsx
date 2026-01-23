import {useEffect, useRef, useState} from "react";
import {
    Card,
    // CardAction,
    CardContent,
    // CardDescription,
    CardFooter,
    CardHeader,
    CardTitle,
} from "@/components/ui/card"
import {Input} from "@/components/ui/input";
import type {MessageObj, PeersIdList} from "@/App";
import {Message} from "@/components/Message";
import {Button} from "@/components/ui/button";
import useVideo from "@/hooks/useVideo.tsx";
import type {sendPeer} from "@/App.tsx";
import {VideoChat} from "@/components/VideoChat.tsx";
import {MembersList} from "@/components/MembersList.tsx";
interface ChatProps extends HubMethods {
    messages: MessageObj[];
    chatroom: string;
    closeButton: () => void;
    chatMembers: string[]

}

interface HubMethods {
    sendMessage: (message: string) => void;
    sendPeer: sendPeer;
    peersId: PeersIdList;
}


export function Chat({messages, chatroom, closeButton, sendMessage, sendPeer, peersId, chatMembers }: ChatProps, ){

    const [message, setMessage] = useState<string>("");


    const {currentUserVideoRef, myPeerId, remoteVideoRefs,
        callHandler, videoCompFlag, closeVideoHandler,
        toggleMicro, toggleCamera, micState} =  useVideo(peersId, sendPeer);

    const lastMessage = useRef<HTMLSpanElement>(null);
    useEffect(() => {
        lastMessage.current!.scrollIntoView();
    }, [messages])







    function onSendMessage(){
        if (message === "") return;
        sendMessage(message);
        setMessage("");
    }


    function handleInputPressEnter(e: React.KeyboardEvent<HTMLInputElement>) {
        if(e.key === 'Enter')
            onSendMessage();
    }



    return(


        <div className={"flex justify-center items-center flex-col min-h-screen "}>


            { videoCompFlag &&
                <VideoChat
                    micState={micState}
                    toggleMicro={toggleMicro}
                    toggleCamera={toggleCamera}
                    peersId={peersId}
                    currentUserVideoRef={currentUserVideoRef}
                    remoteVideoRefs={remoteVideoRefs}
                    myPeerId={myPeerId}
                    closeVideoHandler={closeVideoHandler}
                    className={"mb-5"}/>
            }


            <div className={"flex flex-row"}>
            <Card className={"w-96  bg-neutral-800 border-2 rounded-lg border-amber-200 "}>
                <CardHeader className={"flex justify-between items-center"}>
                    <CardTitle className={"text-amber-200"}>
                        {chatroom}
                    </CardTitle>
                    <div>
                        <Button className={"text-white hover:text-black hover:bg-amber-200/70 mr-4"} onClick={callHandler}>Call</Button>
                        <Button size="icon-sm" className={"hover:bg-red-400/60"} variant="ghost" onClick={closeButton}>X</Button>
                    </div>
                </CardHeader>

                <CardContent className={"overflow-auto max-h-96"}>
                    {messages.map((messageInfo: MessageObj, index: number) => (
                        <Message key={index} messageInfo={messageInfo}/>
                    ))}
                    <span ref={lastMessage}/>
                </CardContent>
                <CardFooter className={"end"}>
                    <Input className={"focus-visible:ring-amber-200 text-white"}
                           type="text"
                           placeholder={"Type your message"}
                           value={message}
                           onKeyDown={handleInputPressEnter}
                           onChange={e => setMessage(e.target.value) }/>

                    <Button className={"ml-4 text-white hover:text-black hover:bg-amber-200/70 "} onClick={onSendMessage}>Send</Button>
                </CardFooter>
            </Card>
                <div className={"mr-auto ml-5"}>
                    <h2 className={"text-xl text-amber-100"}>Members:</h2>
                    <MembersList chatMembers={chatMembers}/>
                </div>


            </div>



        </div>
    );
}