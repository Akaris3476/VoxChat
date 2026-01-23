import {type RefObject, useEffect, useRef, useState, createRef} from "react";
import {MediaConnection, Peer} from "peerjs";
import type {sendPeer} from "@/App.tsx";


export default function useVideo(peersId: string[],sendPeer: sendPeer) {

    const [myPeerId, setMyPeerId] = useState('');
    const currentUserVideoRef = useRef<HTMLVideoElement>(null);

    const remoteVideoRefs =  useRef<Record<string, RefObject<HTMLVideoElement | null>>>({});
    const peerRef = useRef<Peer>(null);

    const [videoCompFlag, setVideoCompFlag] = useState(false);
    const callObjRef = useRef<MediaConnection[]>([]);


    const [micState, setMicState] = useState<boolean>(true);
    const [cameraState, setCameraState] = useState<boolean>(false);

    const mediaStreamRef = useRef<MediaStream>(null)


    useEffect(() => {


        peersId.forEach((peerId )=>{
            if(remoteVideoRefs.current[peerId] === undefined)
                remoteVideoRefs.current[peerId] = createRef<HTMLVideoElement | null>();
        } )

    }, [peersId, remoteVideoRefs]);

    // setup
    const peerSetupDoneRef = useRef(false);
    useEffect(() => {
        const peer = new Peer();
        peerRef.current = peer;




        peer.on('call', async (call) => {

            const ok = window.confirm("You've received a call. Answer?");

            if (!ok){
                call.close()
                return;
            }

            setVideoCompFlag(true);

            try {
                const mediaStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

                mediaStreamRef.current = mediaStream;

                if (currentUserVideoRef.current) {
                    currentUserVideoRef.current.srcObject = mediaStream;
                    currentUserVideoRef.current.play()
                        .catch((err) => {console.error("error displaying current user video", err)});
                }

                call.answer(mediaStream);

                callObjRef.current.push(call);


                const peerId = call.peer

                console.log("got call. caller peerId: ", peerId);

                call.on('stream', (remoteStream) => {

                    if (remoteVideoRefs.current[peerId].current) {
                        remoteVideoRefs.current[peerId].current.srcObject = remoteStream;
                        remoteVideoRefs.current[peerId].current.play()
                            .catch((err) => {console.error("error displaying another user video", err)});
                    }
                });
            } catch (err) {
                console.error('Error getting media:', err);
            }
        });

        peer.on('open', async (id) => {
            console.log("Peer opened:", id);
            setMyPeerId(id);
            await sendPeer(id);
        });

        peerSetupDoneRef.current = true;

        return () => {
            peerSetupDoneRef.current = false;
            peerRef.current = null;
            peer.destroy();
        }
    }, [])

    const call = async (remotePeerId: string) => {
        if (!peerRef.current) return ;

        setVideoCompFlag(true);

        try {
            const mediaStream = await navigator.mediaDevices.getUserMedia({ video: true, audio: true });

            mediaStreamRef.current = mediaStream;

            if (currentUserVideoRef.current) {
                currentUserVideoRef.current.srcObject = mediaStream;
                currentUserVideoRef.current.play()
                    .catch((err) => {console.error("error displaying current user video", err)});
            }

            const callObj = peerRef.current.call(remotePeerId, mediaStream);

            callObjRef.current.push(callObj);


            callObj.on('stream', (remoteStream) => {

                if (remoteVideoRefs.current[remotePeerId].current) {
                    remoteVideoRefs.current[remotePeerId].current.srcObject = remoteStream;
                    remoteVideoRefs.current[remotePeerId].current.play()
                        .catch((err) => {console.error("error displaying another user video", err)});
                }
            });


        } catch (err) {
            console.error('Error getting media', err);
        }
    };

    function callHandler(){

        if (!peerSetupDoneRef.current ||
            (!peersId.includes(myPeerId) && peersId.length <= 1))
        {
            console.error("Error making a call");
            return;
        }


        peersId.forEach((peerId: string) => {
            if (peerId === myPeerId)
                return;

            remoteVideoRefs.current[peerId] = createRef<HTMLVideoElement | null>();

            call(peerId);
        })
    }

    function closeVideoHandler() {
        setVideoCompFlag(false);
        callObjRef.current.forEach((callObj ) => {
            callObj.close()
        })

    }

    function toggleMicro(){
        if (mediaStreamRef.current === null) return;

        const nextState = !micState

        mediaStreamRef.current
            .getAudioTracks()
            .forEach(track => {
            track.enabled = nextState;
        })

        setMicState(nextState);

    }

    function  toggleCamera(){
        if (mediaStreamRef.current === null) return;

        const nextState = !cameraState

        mediaStreamRef.current
            .getVideoTracks()
            .forEach(track => {
                track.enabled = nextState;
            })

        setCameraState(nextState);
    }


    return {currentUserVideoRef, myPeerId, remoteVideoRefs, callHandler, videoCompFlag, closeVideoHandler, toggleMicro, toggleCamera, micState};

}