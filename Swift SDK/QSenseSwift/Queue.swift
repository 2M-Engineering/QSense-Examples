//
//  Queue.swift
//  QSenseSwift
//
//  Created by 2M Engineering ltd on 23/01/2025.
//

private final class QueueNode<T> {
    var value: T;
    var next: QueueNode<T>? = nil;

    init(value: T) { self.value = value }
}

internal final class Queue<T>
{
    private var head: QueueNode<T>? = nil
    private var tail: QueueNode<T>? = nil
    public private(set) var Count : Int = 0;

    internal init() { }
    
    internal func enqueue(newElement: T)
    {
        let oldTail = tail;
        self.tail = QueueNode(value: newElement);
        if  (head == nil)
        {
            head = tail;
        }
        else
        {
            oldTail?.next = self.tail;
        }
        Count+=1;
    }

    internal func dequeue() -> T?
    {
        if (head == nil)
        {
            return nil;
        }
        
        let oldHead = head?.value;
        head = head?.next;
        if (head == nil || head?.next == nil)
        {
            tail = nil;
        }
        Count-=1;
        return oldHead;
    }
}
