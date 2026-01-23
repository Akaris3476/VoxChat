import type {MessageObj} from "@/App";
type MessageProps = {messageInfo: MessageObj};

export function Message({messageInfo}: MessageProps) {

    return(
      <div className={"w-fit my-1"}>
          <span className={"text-amber-200"}>{messageInfo.username}</span>
          <div className={"text-white"}>
              {messageInfo.content}
          </div>
      </div>
    );

}