
interface MembersListProps {
    chatMembers: string[];
}
export function MembersList({chatMembers}: MembersListProps){

    return (
        <div>
        {chatMembers.map((member: string, index: number) => (
            <p key={index} className={"text-lg text-amber-50 mt-2"}>{member}</p>
            ))}
        </div>
    )

}