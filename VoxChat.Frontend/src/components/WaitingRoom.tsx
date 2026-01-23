import { Button } from "@/components/ui/button";
import {
    Card,
    // CardAction,
    CardContent,
    // CardDescription,
    CardFooter,
    // CardHeader,
    CardTitle,
} from "@/components/ui/card"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import {type FormEvent, useEffect} from "react";
import * as React from "react";

type JoinChat = (username: string, chatroom: string) => Promise<void>;
type WaitingRoomProps = { joinChat: JoinChat };

const WaitingRoom = ({ joinChat }: WaitingRoomProps) => {

    const [username, setUsername] = React.useState("");
    const [chatroom, setChatroom] = React.useState("");

    function JoinChatFormHandler(e: FormEvent<HTMLFormElement>)  {
        e.preventDefault();
        joinChat(username, chatroom);
    }
    
    
    
    
    
    return (
        <div className={"flex items-center justify-center min-h-screen flex-col "}>

            <Card className={"bg-neutral-800 p-6 mb-1 text-amber-50 text-2xl border-2 border-amber-200 rounded-lg"}>


                <CardTitle className={ "border-amber-200"} >VoxChat</CardTitle>

                <form onSubmit={JoinChatFormHandler}>
                    <CardContent className={"px-0"}>

                         <div className="mb-4">
                             <Label className="mb-1 text-lg">Username</Label>
                             <Input onChange={(e) => setUsername(e.target.value)} placeholder={"Type your username"} type="username" required></Input>
                         </div>

                        <div className="mb-2">
                            <Label className="mb-1 text-lg" >Chatroom</Label>
                            <Input type="chatroom" onChange={(e) => setChatroom(e.target.value)} placeholder={"Type chatroom name"}  required></Input>
                        </div>

                    </CardContent>


                    <CardFooter className={"px-0"}>
                         <Button type="submit">Join</Button>
                    </CardFooter>
                </form>

            </Card>

        </div>
    );

}
export default WaitingRoom;