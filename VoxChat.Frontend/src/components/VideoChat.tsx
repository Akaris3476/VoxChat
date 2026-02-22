import {type RefObject, useEffect, useMemo} from "react";
import placeholder from "@/assets/video_placeholder.jpg"
import type {PeersIdList} from "@/App.tsx";
import {Button} from "@/components/ui/button.tsx";
interface VideoChatProps {

    peersId: PeersIdList;
    currentUserVideoRef: RefObject<HTMLVideoElement | null>;
    className: string;
    remoteVideoRefs: RefObject<Record<string, RefObject<HTMLVideoElement | null>>>;
    myPeerId: string;
    closeVideoHandler: () => void;
    toggleMicro: () => void;
    toggleCamera: () => void;
    micState: boolean
}

export function VideoChat({className, currentUserVideoRef, peersId, remoteVideoRefs, myPeerId,
                              closeVideoHandler, toggleMicro, toggleCamera, micState}: VideoChatProps) {

    const videoWidth = 300;


    const otherPeerIds = useMemo(() => {
        return peersId.filter((peerId: string) => peerId !== myPeerId);
    }, [peersId, myPeerId]);



    function toggleMicroHandler(){
        toggleMicro();
    }

    function toggleVideoHandler(){
        toggleCamera();
    }
    return (
        <div className={className}>

            {!micState && <p>Your microphone is off</p>  }
            <div className="flex flex-row justify-center gap-5 mb-4">
                <div>
                    <video  autoPlay playsInline
                            muted
                            style={{ width: videoWidth }}
                            ref={currentUserVideoRef}
                            poster={placeholder}/>
                </div>

                {/*<h1>{myPeerId}</h1>*/}
                {otherPeerIds.map((peerId) => (
                    <video key={peerId}
                           autoPlay playsInline
                           style={{ width: videoWidth }}
                           ref={remoteVideoRefs.current[peerId]}
                           poster={placeholder}/>
                ))}
            </div>

            <div className="flex flex-row justify-center gap-4 ">
                <Button className={"rounded-4xl h-15 w-15 text-white hover:text-black hover:bg-amber-200/70"}
                onClick={toggleMicroHandler} >Micro</Button>
                <Button className={"rounded-4xl h-15 w-15 text-white hover:text-black hover:bg-amber-200/70"}
                onClick={toggleVideoHandler} >Video</Button>


                <Button className={"rounded-4xl h-15 w-15 hover:bg-red-400/60"} onClick={closeVideoHandler}>X</Button>

            </div>
        </div>
    )
}